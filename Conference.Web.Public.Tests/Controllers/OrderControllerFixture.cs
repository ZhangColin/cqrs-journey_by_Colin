using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Web.Mvc;
using System.Web.Routing;
using Conference.Web.Public.Controllers;
using Infrastructure.Messaging;
using Moq;
using NUnit.Framework;
using Registration.Commands;
using Registration.Contracts;
using Registration.ReadModel;

namespace Conference.Web.Public.Tests.Controllers {
    [TestFixture]
    public class OrderControllerFixture {
        private OrderController _sut;
        private Mock<IOrderDao> _orderDao;
        private List<ICommand> _commands;

        private OrderSeats _orderSeats;

        [SetUp]
        public void Setup() {
            this._orderDao = new Mock<IOrderDao>();
            this._commands = new List<ICommand>();

            var bus = new Mock<ICommandBus>();
            bus.Setup(x => x.Send(It.IsAny<Envelope<ICommand>>()))
                .Callback<Envelope<ICommand>>(x => _commands.Add(x.Body));
            bus.Setup(x => x.Send(It.IsAny<IEnumerable<Envelope<ICommand>>>()))
                .Callback<IEnumerable<Envelope<ICommand>>>(x => _commands.AddRange(x.Select(e => e.Body)));

            this._sut = new OrderController(Mock.Of<IConferenceDao>(), this._orderDao.Object, bus.Object);

            var routeData = new RouteData();
            routeData.Values.Add("conferenceCode", "conference");

            this._sut.ControllerContext = Mock.Of<ControllerContext>(x => x.RouteData == routeData);

            this._orderSeats = new OrderSeats() {
                AssignmentsId = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Seats = {
                    new OrderSeat(0, "General") {
                        Attendee = new PersonalInfo {
                            Email = "a@a.com",
                            FirstName = "A",
                            LastName = "Z"
                        }
                    },
                    new OrderSeat(1, "Precon")
                }
            };
            this._orderDao.Setup(r => r.FindOrderSeats(this._orderSeats.OrderId)).Returns(this._orderSeats);
        }

        [Test]
        public void When_displaying_invalid_order_id_then_redirects_to_find() {
            // Act
            var result = (RedirectToRouteResult)this._sut.Display(Guid.NewGuid());

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual(null, result.RouteValues["controller"]);
            Assert.AreEqual("Find", result.RouteValues["action"]);
            Assert.AreEqual("conference", result.RouteValues["conferenceCode"]);
        }

        [Test]
        public void When_find_order_then_renders_view() {
            // Act
            var result = (ViewResult)this._sut.Find();

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual("", result.ViewName);
        }

        [Test]
        public void When_find_order_with_valid_email_and_access_code_then_redirects_to_display_with_order_id() {
            // Arrange
            var orderId = Guid.NewGuid();
            this._orderDao.Setup(r => r.LocateOrder("info@contoso.com", "asdf")).Returns(orderId);

            // Act
            var result = (RedirectToRouteResult)this._sut.Find("info@contoso.com", "asdf");

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual(null, result.RouteValues["controller"]);
            Assert.AreEqual("Display", result.RouteValues["action"]);
            Assert.AreEqual("conference", result.RouteValues["conferenceCode"]);
            Assert.AreEqual(orderId, result.RouteValues["orderId"]);
        }

        [Test]
        public void When_find_order_with_invalid_locator_then_redirects_to_find() {
            // Act
            var result = (RedirectToRouteResult)this._sut.Find("info@contoso.com", "asdf");

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual(null, result.RouteValues["controller"]);
            Assert.AreEqual("Find", result.RouteValues["action"]);
            Assert.AreEqual("conference", result.RouteValues["conferenceCode"]);
        }

        [Test]
        public void When_display_valid_order_then_renders_view_with_priced_order() {
            // Arrange
            var orderId = Guid.NewGuid();
            var dto = new PricedOrder() {
                OrderId = orderId,
                Total = 200
            };

            this._orderDao.Setup(r => r.FindPricedOrder(orderId)).Returns(dto);

            // Act
            var result = (ViewResult)this._sut.Display(orderId);

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual(dto, result.Model);
        }

        [Test]
        public void When_displaying_seat_assignment_then_displays_order_seats() {
            // Act
            var result = (ViewResult)this._sut.AssignSeats(this._orderSeats.OrderId);

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual(this._orderSeats, result.Model);
        }

        [Test]
        public void When_no_seat_assignments_for_order_then_redirects_to_find() {
            // Act
            var result = (RedirectToRouteResult)this._sut.AssignSeats(Guid.NewGuid());

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual(null, result.RouteValues["controller"]);
            Assert.AreEqual("Find", result.RouteValues["action"]);
            Assert.AreEqual("conference", result.RouteValues["conferenceCode"]);
        }

        [Test]
        public void When_assigning_seat_non_existent_order_then_redirects_to_find() {
            // Act
            var result = (RedirectToRouteResult)this._sut.AssignSeats(Guid.NewGuid(), new List<OrderSeat>());

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual(null, result.RouteValues["controller"]);
            Assert.AreEqual("Find", result.RouteValues["action"]);
            Assert.AreEqual("conference", result.RouteValues["conferenceCode"]);
        }

        [Test]
        public void When_seat_assigned_then_sends_assign_command() {
            // Act
            var result = (RedirectToRouteResult)this._sut.AssignSeats(this._orderSeats.OrderId, new List<OrderSeat>() {
                new OrderSeat(1, "Precon") {
                    Attendee = new PersonalInfo() {
                        Email = "a@a.com",
                        FirstName = "A",
                        LastName = "Z"
                    }
                }
            });

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual("Display", result.RouteValues["action"]);

            var cmd = this._commands.OfType<AssignSeat>().Single();
            Assert.AreEqual(this._orderSeats.AssignmentsId, cmd.SeatAssignmentsId);
            Assert.AreEqual(1, cmd.Position);
            Assert.AreEqual("a@a.com", cmd.Attendee.Email);
            Assert.AreEqual("A", cmd.Attendee.FirstName);
            Assert.AreEqual("Z", cmd.Attendee.LastName);
        }

        [Test]
        public void When_invalid_position_seat_assigned_then_ignores_it() {
            // Act
            var result = (RedirectToRouteResult)this._sut.AssignSeats(this._orderSeats.OrderId, new List<OrderSeat>() {
                new OrderSeat(5, "Precon") {
                    Attendee = new PersonalInfo() {
                        Email = "a@a.com",
                        FirstName = "A",
                        LastName = "Z"
                    }
                }
            });

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual("Display", result.RouteValues["action"]);

            Assert.AreEqual(0, this._commands.Count);
        }

        [Test]
        public void When_null_seat_assigned_then_ignores_is() {
            // Act
            var result = (RedirectToRouteResult)this._sut.AssignSeats(this._orderSeats.OrderId, new List<OrderSeat>() {
                null
            });

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual("Display", result.RouteValues["action"]);

            Assert.AreEqual(0, this._commands.Count);
        }

        [Test]
        public void When_seat_assigned_email_remains_null_then_ignores_it() {
            // Act
            var result = (RedirectToRouteResult)this._sut.AssignSeats(this._orderSeats.OrderId, new List<OrderSeat>() {
                new OrderSeat() {Position = 1, Attendee = new PersonalInfo()}
            });

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual("Display", result.RouteValues["action"]);

            Assert.AreEqual(0, this._commands.Count);
        }

        [Test]
        public void When_previously_assigned_seat_email_becomes_null_then_sends_unassign_command() {
            // Act
            var result = (RedirectToRouteResult)this._sut.AssignSeats(this._orderSeats.OrderId, new List<OrderSeat>() {
                new OrderSeat() {
                    Position = 0,
                    Attendee = new PersonalInfo() {
                        Email = null,
                        FirstName = "A",
                        LastName = "Z"
                    }
                }
            });

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual("Display", result.RouteValues["action"]);

            var cmd = this._commands.OfType<UnassignSeat>().Single();
            Assert.AreEqual(this._orderSeats.AssignmentsId, cmd.SeatAssignmentsId);
            Assert.AreEqual(0, cmd.Position);
        }
        
        [Test]
        public void When_previously_assigned_seat_firstname_changes_then_sends_assign_command() {
            // Act
            var result = (RedirectToRouteResult)this._sut.AssignSeats(this._orderSeats.OrderId, new List<OrderSeat>() {
                new OrderSeat() {
                    Position = 0,
                    Attendee = new PersonalInfo() {
                        Email = "a@a.com",
                        FirstName = "B",
                        LastName = "Z"
                    }
                }
            });

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual("Display", result.RouteValues["action"]);

            var cmd = this._commands.OfType<AssignSeat>().Single();
            Assert.AreEqual(this._orderSeats.AssignmentsId, cmd.SeatAssignmentsId);
            Assert.AreEqual(0, cmd.Position);
            Assert.AreEqual("a@a.com", cmd.Attendee.Email);
            Assert.AreEqual("B", cmd.Attendee.FirstName);
            Assert.AreEqual("Z", cmd.Attendee.LastName);
        }
    }
}