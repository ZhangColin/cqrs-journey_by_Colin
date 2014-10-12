using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Conference.Web.Public.Controllers;
using Conference.Web.Public.Models;
using Infrastructure.Messaging;
using Moq;
using NUnit.Framework;
using Payments.Contracts.Commands;
using Registration.Commands;
using Registration.Contracts;
using Registration.ReadModel;

namespace Conference.Web.Public.Tests.Controllers {
    [TestFixture]
    public class RegistrationControllerFixture {
        private RegistrationController _sut;
        private ICommandBus _commandBus;
        private IOrderDao _orderDao;
        private IConferenceDao _conferenceDao ;
        private ConferenceAlias _conferenceAlias;
        private RouteCollection _routes;
        private RouteData _routeData;
        private Mock<HttpRequestBase> _requestMock;
        private Mock<HttpResponseBase> _responseMock;

        [SetUp]
        public void Setup() {
            this._conferenceAlias = new ConferenceAlias() {
                Id = Guid.NewGuid(), Code = "TestConferenceCode", Name = "Test Conference name"
            };

            this._commandBus = Mock.Of<ICommandBus>();
            this._conferenceDao = Mock.Of<IConferenceDao>(
                x => x.GetConferenceAlias(_conferenceAlias.Code) == _conferenceAlias);
            this._orderDao = Mock.Of<IOrderDao>();

            this._routes = new RouteCollection();

            this._routeData = new RouteData();
            this._routeData.Values.Add("conferenceCode", _conferenceAlias.Code);

            var requestMock = new Mock<HttpRequestBase>(MockBehavior.Strict);
            requestMock.SetupGet(x => x.ApplicationPath).Returns("/");
            requestMock.SetupGet(x => x.Url).Returns(new Uri("http://localhost/request", UriKind.Absolute));
            requestMock.SetupGet(x => x.ServerVariables).Returns(new NameValueCollection());

            var responseMock = new Mock<HttpResponseBase>(MockBehavior.Strict);
            responseMock.Setup(x => x.ApplyAppPathModifier(It.IsAny<string>())).Returns<string>(s => s);

            var context =
                Mock.Of<HttpContextBase>(c => c.Request == requestMock.Object && c.Response == responseMock.Object);

            this._sut = new RegistrationController(this._commandBus, this._orderDao, this._conferenceDao);
            this._sut.ConferenceAlias = _conferenceAlias;
            this._sut.ConferenceCode = _conferenceAlias.Code;
            this._sut.ControllerContext = new ControllerContext(context, this._routeData, this._sut);
            this._sut.Url = new UrlHelper(new RequestContext(context, this._routeData), this._routes);
        }

        [Test]
        public void When_starting_registration_then_returns_view_with_registration_for_conference() {
            var seatTypeId = Guid.NewGuid();
            var seats = new[] {new SeatType(seatTypeId, _conferenceAlias.Id, "Test Seat", "Description", 10, 50)};

            // Arrange 
            Mock.Get(this._conferenceDao).Setup(r => r.GetPublishedSeatTypes(this._conferenceAlias.Id)).Returns(seats);

            // Act
            var result = (ViewResult)this._sut.StartRegistration().Result;

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual("", result.ViewName);

            var resultModel = (OrderViewModel)result.Model;
            Assert.NotNull(resultModel);
            Assert.AreEqual(1, resultModel.Items.Count);
            Assert.AreEqual("Test Seat", resultModel.Items[0].SeatType.Name);
            Assert.AreEqual("Description", resultModel.Items[0].SeatType.Description);
            Assert.AreEqual(0, resultModel.Items[0].OrderItem.RequestedSeats);
            Assert.AreEqual(0, resultModel.Items[0].OrderItem.ReservedSeats);
        }

        [Test]
        public void when_specifying_seats_for_a_valid_registration_then_places_registration_and_redirects_to_action() {
            var seatTypeId = Guid.NewGuid();
            var seats = new[]
            {new SeatType(seatTypeId, _conferenceAlias.Id, "Test Seat", "Description", 10, 50) {AvailableQuantity = 50}};

            // Arrange
            Mock.Get(this._conferenceDao).Setup(r => r.GetPublishedSeatTypes(_conferenceAlias.Id)).Returns(seats);

            var orderId = Guid.NewGuid();

            Mock.Get(this._orderDao).Setup(r => r.FindDraftOrder(orderId)).Returns(new DraftOrder(orderId, _conferenceAlias.Id, DraftOrder.States.PendingReservation));

            var registration =
                new RegisterToConference {
                    OrderId = orderId,
                    Seats = { new SeatQuantity(seatTypeId, 10) }
                };

            // Act
            var result = (RedirectToRouteResult)this._sut.StartRegistration(registration, 0);

            // Assert
            Assert.AreEqual(null, result.RouteValues["controller"]);
            Assert.AreEqual("SpecifyRegistrantAndPaymentDetails", result.RouteValues["action"]);
            Assert.AreEqual(_conferenceAlias.Code, result.RouteValues["conferenceCode"]);
            Assert.AreEqual(orderId, result.RouteValues["orderId"]);

            Mock.Get<ICommandBus>(this._commandBus)
                .Verify(b => b.Send(It.Is<Envelope<ICommand>>(e =>
                    ((RegisterToConference)e.Body).ConferenceId == _conferenceAlias.Id
                        && ((RegisterToConference)e.Body).OrderId == orderId
                        && ((RegisterToConference)e.Body).Seats.Count == 1
                        && ((RegisterToConference)e.Body).Seats.ElementAt(0).Quantity == 10
                        && ((RegisterToConference)e.Body).Seats.ElementAt(0).SeatType == seatTypeId)),
                    Times.Once());
        }

        [Test]
        public void when_initiating_payment_for_a_partially_reserved_order_then_redirects_back_to_seat_selection() {
            var seatTypeId = Guid.NewGuid();
            var seats = new[] { new SeatType(seatTypeId, _conferenceAlias.Id, "Test Seat", "Description", 10, 50) };

            Mock.Get(this._conferenceDao).Setup(r => r.GetPublishedSeatTypes(_conferenceAlias.Id)).Returns(seats);

            var orderId = Guid.NewGuid();
            var orderVersion = 10;

            Mock.Get(this._orderDao)
                .Setup(r => r.FindDraftOrder(orderId))
                .Returns(
                    new DraftOrder(orderId, _conferenceAlias.Id, DraftOrder.States.PartiallyReserved, orderVersion) {
                        Lines = { new DraftOrderItem(seatTypeId, 10) { ReservedSeats = 5 } }
                    });

            var result = (RedirectToRouteResult)this._sut.StartPayment(orderId, RegistrationController.ThirdPartyProcessorPayment, orderVersion - 1).Result;

            Assert.AreEqual(null, result.RouteValues["controller"]);
            Assert.AreEqual("StartRegistration", result.RouteValues["action"]);
            Assert.AreEqual(this._conferenceAlias.Code, result.RouteValues["conferenceCode"]);
            Assert.AreEqual(orderId, result.RouteValues["orderId"]);
            Assert.AreEqual(orderVersion, result.RouteValues["orderVersion"]);
        }

        [Test]
        public void when_displaying_payment_and_registration_information_for_a_not_yet_updated_order_then_shows_wait_page() {
            var seatTypeId = Guid.NewGuid();
            var seats = new[] { new SeatType(seatTypeId, _conferenceAlias.Id, "Test Seat", "Description", 10, 50) };

            Mock.Get(this._conferenceDao).Setup(r => r.GetPublishedSeatTypes(_conferenceAlias.Id)).Returns(seats);

            var orderId = Guid.NewGuid();
            var orderVersion = 10;

            Mock.Get<IOrderDao>(this._orderDao)
                .Setup(d => d.FindPricedOrder(orderId))
                .Returns(new PricedOrder { OrderId = orderId, Total = 100, OrderVersion = orderVersion });
            var result = (ViewResult)this._sut.SpecifyRegistrantAndPaymentDetails(orderId, orderVersion).Result;

            Assert.AreEqual("PricedOrderUnknown", result.ViewName);
        }

        [Test]
        public void when_displaying_payment_and_registration_information_for_a_fully_reserved_order_then_shows_input_page() {
            var seatTypeId = Guid.NewGuid();
            var seats = new[] { new SeatType(seatTypeId, _conferenceAlias.Id, "Test Seat", "Description", 10, 50) { AvailableQuantity = 50 } };

            Mock.Get(this._conferenceDao).Setup(r => r.GetPublishedSeatTypes(_conferenceAlias.Id)).Returns(seats);

            var orderId = Guid.NewGuid();
            var orderVersion = 10;

            Mock.Get(this._orderDao)
                .Setup(r => r.FindDraftOrder(orderId))
                .Returns(
                    new DraftOrder(orderId, _conferenceAlias.Id, DraftOrder.States.ReservationCompleted, orderVersion) {
                        Lines = { new DraftOrderItem(seatTypeId, 10) { ReservedSeats = 5 } }
                    });
            Mock.Get(this._orderDao)
                .Setup(r => r.FindPricedOrder(orderId))
                .Returns(new PricedOrder { OrderId = orderId, OrderVersion = orderVersion, ReservationExpirationDate = DateTime.UtcNow.AddMinutes(1) });

            var result = (ViewResult)this._sut.SpecifyRegistrantAndPaymentDetails(orderId, orderVersion - 1).Result;

            Assert.AreEqual(string.Empty, result.ViewName);
            var model = (RegistrationViewModel)result.Model;
        }

        [Test]
        public void when_specifying_registrant_and_credit_card_payment_details_for_a_valid_registration_then_sends_commands_and_redirects_to_payment_action() {
            var orderId = Guid.NewGuid();
            var command = new AssignRegistrantDetails {
                OrderId = orderId,
                Email = "info@contoso.com",
                FirstName = "First Name",
                LastName = "Last Name",
            };
            InitiateThirdPartyProcessorPayment paymentCommand = null;

            // Arrange
            var seatId = Guid.NewGuid();

            var order = new DraftOrder(orderId, _conferenceAlias.Id, DraftOrder.States.ReservationCompleted, 10);
            order.Lines.Add(new DraftOrderItem(seatId, 5) { ReservedSeats = 5 });
            Mock.Get<IOrderDao>(this._orderDao)
                .Setup(d => d.FindDraftOrder(orderId))
                .Returns(order);
            Mock.Get<IOrderDao>(this._orderDao)
                .Setup(d => d.FindPricedOrder(orderId))
                .Returns(new PricedOrder { OrderId = orderId, Total = 100, OrderVersion = 10 });

            Mock.Get<ICommandBus>(this._commandBus)
                .Setup(b => b.Send(It.IsAny<Envelope<ICommand>>()))
                .Callback<Envelope<ICommand>>(
                    es => { if (es.Body is InitiateThirdPartyProcessorPayment) paymentCommand = (InitiateThirdPartyProcessorPayment)es.Body; });

            this._routes.MapRoute("ThankYou", "thankyou", new { controller = "Registration", action = "ThankYou" });
            this._routes.MapRoute("SpecifyRegistrantAndPaymentDetails", "checkout", new { controller = "Registration", action = "SpecifyRegistrantAndPaymentDetails" });

            // Act
            var result =
                (RedirectToRouteResult)this._sut.SpecifyRegistrantAndPaymentDetails(command, RegistrationController.ThirdPartyProcessorPayment, 0).Result;

            // Assert
            Mock.Get<ICommandBus>(this._commandBus)
                .Verify(b => b.Send(It.Is<Envelope<ICommand>>(es => es.Body == command)), Times.Once());

            Assert.NotNull(paymentCommand);
            Assert.AreEqual(_conferenceAlias.Id, paymentCommand.ConferenceId);
            Assert.AreEqual(orderId, paymentCommand.PaymentSourceId);
            Assert.IsTrue(99.9m<=paymentCommand.TotalAmount && paymentCommand.TotalAmount<= 100.1m);

            Assert.AreEqual("Payment", result.RouteValues["controller"]);
            Assert.AreEqual("ThirdPartyProcessorPayment", result.RouteValues["action"]);
            Assert.AreEqual(this._conferenceAlias.Code, result.RouteValues["conferenceCode"]);
            Assert.AreEqual(paymentCommand.PaymentId, result.RouteValues["paymentId"]);
            Assert.True(((string)result.RouteValues["paymentAcceptedUrl"]).StartsWith("/thankyou"));
            Assert.True(((string)result.RouteValues["paymentRejectedUrl"]).StartsWith("/checkout"));
        }

        [Test]
        public void when_specifying_registrant_and_credit_card_payment_details_for_a_non_yet_updated_order_then_shows_wait_page() {
            var orderId = Guid.NewGuid();
            var orderVersion = 10;
            var command = new AssignRegistrantDetails {
                OrderId = orderId,
                Email = "info@contoso.com",
                FirstName = "First Name",
                LastName = "Last Name",
            };
            Guid paymentId = Guid.Empty;

            var seatTypeId = Guid.NewGuid();

            Mock.Get(this._orderDao)
                .Setup(r => r.FindDraftOrder(orderId))
                .Returns(
                    new DraftOrder(orderId, _conferenceAlias.Id, DraftOrder.States.Confirmed, orderVersion) {
                        Lines = { new DraftOrderItem(seatTypeId, 10) { ReservedSeats = 5 } }
                    });
            Mock.Get<IOrderDao>(this._orderDao)
                .Setup(d => d.FindPricedOrder(orderId))
                .Returns(new PricedOrder { OrderId = orderId, Total = 100, OrderVersion = orderVersion + 1 });
            var result = (ViewResult)this._sut.SpecifyRegistrantAndPaymentDetails(command, RegistrationController.ThirdPartyProcessorPayment, orderVersion).Result;

            Assert.AreEqual("ReservationUnknown", result.ViewName);
        }
    }
}