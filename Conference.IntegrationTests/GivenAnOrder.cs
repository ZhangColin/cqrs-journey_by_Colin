using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.InteropServices;
using Infrastructure.Messaging;
using Moq;
using NUnit.Framework;
using Registration.Contracts;
using Registration.Contracts.Events;

namespace Conference.IntegrationTests {
    [TestFixture]
    public class GivenAnOrder {
        private string _dbName = "ConferenceServiceFixture_" + Guid.NewGuid();
        private ConferenceService _service;
        private List<IEvent> _busEvents;
        private ConferenceInfo _conference;

        private OrderPlaced _placed;
        private OrderEventHandler _eventHandler;

        [SetUp]
        public void Setup() {
            // 初始化会议数据库
            using (var context = new ConferenceContext(this._dbName)) {
                if (context.Database.Exists()) {
                    context.Database.Delete();
                }
                context.Database.Create();
            }

            // 模拟事件总线
            this._busEvents = new List<IEvent>();
            var busMock = new Mock<IEventBus>();
            busMock.Setup(b => b.Publish(It.IsAny<Envelope<IEvent>>()))
                .Callback<Envelope<IEvent>>(e => this._busEvents.Add(e.Body));
            busMock.Setup(b => b.Publish(It.IsAny<IEnumerable<Envelope<IEvent>>>()))
                .Callback<IEnumerable<Envelope<IEvent>>>(es => this._busEvents.AddRange(es.Select(e => e.Body)));

            this._service = new ConferenceService(busMock.Object, this._dbName);

            this._conference = new ConferenceInfo() {
                OwnerEmail = "test@contoso.com",
                OwnerName = "test owner",
                AccessCode = "qwerty",
                Name = "test conference",
                Description = "test conference description",
                Location = "redmond",
                Slug = "test",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.Add(TimeSpan.FromDays(2)),
                Seats = {
                    new SeatType() {
                        Name = "general", 
                        Description = "general description", 
                        Price = 100, 
                        Quantity = 10
                    }
                }
            };

            this._service.CreateConference(this._conference);

            this._placed = new OrderPlaced() {
                ConferenceId = Guid.NewGuid(),
                SourceId = Guid.NewGuid(),
                AccessCode = "asdf"
            };

            this._eventHandler = new OrderEventHandler(() => new ConferenceContext(_dbName));
            this._eventHandler.Handle(_placed);
        }

        [TearDown]
        public void TearDown() {
            using (var context = new ConferenceContext(this._dbName)) {
                if (context.Database.Exists()) {
                    context.Database.Delete();
                }
            }
        }

        [Test]
        public void When_order_totals_calculated_then_updates_order_total() {
            var e = new OrderExpired() {
                SourceId = _placed.SourceId
            };

            this._eventHandler.Handle(e);

            var order = FindOrder(e.SourceId);
            Assert.Null(order);
        }

        [Test]
        public void When_order_expired_then_deletes_entity() {
            var e = new OrderTotalsCalculated() {
                SourceId = _placed.SourceId,
                Total = 10
            };

            this._eventHandler.Handle(e);

            var order = this.FindOrder(e.SourceId);
            Assert.AreEqual(e.Total, order.TotalAmount);
        }

        [Test]
        public void When_order_registrant_assigned_then_sets_registrant() {
            var e = new OrderRegistrantAssigned() {
                SourceId = _placed.SourceId,
                Email = "test@contoso.com",
                FirstName = "A",
                LastName = "Z"
            };

            this._eventHandler.Handle(e);

            var order = this.FindOrder(e.SourceId);

            Assert.AreEqual(e.Email, order.RegistrantEmail);
            Assert.True(order.RegistrantName.Contains("A"));
            Assert.True(order.RegistrantName.Contains("Z"));
        }

        [Test]
        public void When_order_confirmed_then_confirms_order() {
            var e = new OrderConfirmed() {
                SourceId = _placed.SourceId
            };

            this._eventHandler.Handle(e);

            var order = this.FindOrder(e.SourceId);

            Assert.AreEqual(Order.OrderStatus.Paid, order.Status);
        }

        [Test]
        public void When_seat_assigned_then_adds_order_seat() {
            this._eventHandler.Handle(new SeatAssignmentsCreated() {
                SourceId = _placed.SourceId, OrderId = _placed.SourceId
            });

            var e = new SeatAssigned(_placed.SourceId) {
                Attendee = new PersonalInfo() {
                    Email = "test@contoso.com",
                    FirstName = "A",
                    LastName = "Z"
                },
                SeatType = this._conference.Seats.First().Id
            };

            this._eventHandler.Handle(e);

            var order = this.FindOrder(e.SourceId);
            Assert.AreEqual(1, order.Seats.Count);
        }

        [Test]
        public void When_seat_asignee_updated_then_updates_order_seat() {
            this._eventHandler.Handle(new SeatAssignmentsCreated() {
                SourceId = _placed.SourceId,
                OrderId = _placed.SourceId
            });

            var e = new SeatAssigned(_placed.SourceId) {
                Attendee = new PersonalInfo() {
                    Email = "test@contoso.com",
                    FirstName = "A",
                    LastName = "Z"
                },
                SeatType = this._conference.Seats.First().Id
            };

            this._eventHandler.Handle(e);

            e.Attendee.LastName = "B";

            this._eventHandler.Handle(e);

            var order = this.FindOrder(e.SourceId);

            Assert.AreEqual(1, order.Seats.Count);
            Assert.AreEqual("B", order.Seats.First().Attendee.LastName);
        }

        private Order FindOrder(Guid orderId) {
            using(var context = new ConferenceContext(_dbName)) {
                return context.Orders.Include(o => o.Seats).FirstOrDefault(o => o.Id == orderId);
            }
        }
    }
}