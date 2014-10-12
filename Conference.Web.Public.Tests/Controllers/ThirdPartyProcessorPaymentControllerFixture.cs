using System.Web.Mvc;
using Conference.Web.Public.Areas.ThirdPartyProcessor.Controllers;
using NUnit.Framework;

namespace Conference.Web.Public.Tests.Controllers {
    [TestFixture]
    public class ThirdPartyProcessorPaymentControllerFixture {
        private ThirdPartyProcessorPaymentController _sut;

        [SetUp]
        public void Setup() {
            this._sut = new ThirdPartyProcessorPaymentController();
        }

        [Test]
        public void When_initiating_payment_then_returns_payment_view() {
            var result = (ViewResult)this._sut.Pay("item", 100, "return", "cancelreturn");

            Assert.AreEqual(this._sut.ViewBag.ReturnUrl, "return");
            Assert.AreEqual(this._sut.ViewBag.CancelReturnUrl, "cancelreturn");
            Assert.AreEqual(this._sut.ViewBag.ItemName, "item");
            Assert.AreEqual(this._sut.ViewBag.ItemAmount, 100m);
        }

        [Test]
        public void When_accepting_payment_then_redirects_to_return_url() {
            var result = (RedirectResult)this._sut.Pay("accepted", "return", "cancelRetrun");

            Assert.AreEqual("return", result.Url);
            Assert.IsFalse(result.Permanent);
        }
        
        [Test]
        public void When_rejecting_payment_then_redirects_to_return_url() {
            var result = (RedirectResult)this._sut.Pay("rejected", "return", "cancelRetrun");

            Assert.AreEqual("cancelRetrun", result.Url);
            Assert.IsFalse(result.Permanent);
        }
    }
}