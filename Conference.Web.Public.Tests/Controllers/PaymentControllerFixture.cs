using System;
using System.Collections.Specialized;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Conference.Web.Public.Controllers;
using Infrastructure.Messaging;
using Moq;
using NUnit.Framework;
using Payments.Contracts.Commands;
using Payments.ReadModel;

namespace Conference.Web.Public.Tests.Controllers {
    [TestFixture]
    public class PaymentControllerFixture {
        private PaymentController _sut;
        private Mock<ICommandBus> _commandBus;
        private Mock<IPaymentDao> _paymentDao;

        [SetUp]
        public void Setup() {
            var routes = new RouteCollection();
            routes.MapRoute("PaymentAccept", "accept",
                new {controller = "Payment", action = "ThirdPartyProcessorPaymentAccepted"});
            routes.MapRoute("PaymentReject", "accept",
                new {controller = "Payment", action = "ThirdPartyProcessorPaymentRejected"});
            routes.MapRoute("Pay", "payment", new {controller = "ThirdPartyProcessorPayment", action = "Pay"});

            var requestMock = new Mock<HttpRequestBase>(MockBehavior.Strict);
            requestMock.SetupGet(x => x.ApplicationPath).Returns("/");
            requestMock.SetupGet(x => x.Url).Returns(new Uri("http://localhost/request", UriKind.Absolute));
            requestMock.SetupGet(x => x.ServerVariables).Returns(new NameValueCollection());

            var responseMock = new Mock<HttpResponseBase>(MockBehavior.Strict);
            responseMock.Setup(x => x.ApplyAppPathModifier(It.IsAny<string>())).Returns<string>(s => s);

            var context =
                Mock.Of<HttpContextBase>(c => c.Request == requestMock.Object && c.Response == responseMock.Object);

            this._commandBus = new Mock<ICommandBus>();
            this._paymentDao = new Mock<IPaymentDao>();

            this._sut = new PaymentController(this._commandBus.Object, this._paymentDao.Object);
            this._sut.ControllerContext = new ControllerContext(context, new RouteData(), this._sut);
            this._sut.Url = new UrlHelper(new RequestContext(context, new RouteData()), routes);
        }

        [Test]
        public void When_initiating_third_party_processor_payment_then_redirects_to_thid_party() {
            // Arrange
            var paymentId = Guid.NewGuid();
            this._paymentDao.Setup(dao => dao.GetThirdPartyProcessorPaymentDetails(It.IsAny<Guid>()))
                .Returns(new ThirdPartyProcessorPaymentDetails(Guid.NewGuid(),
                    Payments.ThirdPartyProcessorPayment.States.Initiated, Guid.NewGuid(), "payment", 100));

            // Act
            var result =
                (RedirectResult)this._sut.ThirdPartyProcessorPayment("conferenc", paymentId, "accept", "reject");

            // Assert
            Assert.IsFalse(result.Permanent);
            Assert.IsTrue(result.Url.StartsWith("/payment"));
        }

        [Test]
        public void When_payment_is_accepted_then_redirects_to_order() {
            // Arrange
            var paymentId = Guid.NewGuid();

            // Act
            var result = (RedirectResult)this._sut.ThirdPartyProcessorPaymentAccepted("conference", paymentId, "accept");

            // Assert
            Assert.AreEqual("accept", result.Url);
            Assert.IsFalse(result.Permanent);
        }

        [Test]
        public void When_payment_is_accepted_then_publishes_command() {
            // Arrange
            var paymentId = Guid.NewGuid();

            // Act
            var result = (RedirectResult)this._sut.ThirdPartyProcessorPaymentAccepted("conference", paymentId, "accept");

            // Assert
            this._commandBus.Verify(cb => cb.Send(
                It.Is<Envelope<ICommand>>(e => ((CompleteThirdPartyProcessorPayment)e.Body).PaymentId == paymentId)),
                Times.Once);
        }

        [Test]
        public void When_payment_is_rejected_then_redirects_to_order() {
            // Arrange
            var paymentId = Guid.NewGuid();

            // Act
            var result = (RedirectResult)this._sut.ThirdPartyProcessorPaymentRejected("conference", paymentId, "reject");

            // Assert
            Assert.AreEqual("reject", result.Url);
            Assert.IsFalse(result.Permanent);
        }

        [Test]
        public void When_payment_is_rejected_then_publishes_command() {
            // Arrange
            var paymentId = Guid.NewGuid();

            // Act
            var result = (RedirectResult)this._sut.ThirdPartyProcessorPaymentRejected("conference", paymentId, "reject");

            // Assert
            this._commandBus.Verify(cb => cb.Send(
                It.Is<Envelope<ICommand>>(e => ((CancelThirdPartyProcessorPayment)e.Body).PaymentId == paymentId)),
                Times.Once);
        }
    }
}