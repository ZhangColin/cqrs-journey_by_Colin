using System;
using System.Collections.Specialized;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using Registration.ReadModel;

namespace Conference.Web.Public.Tests {
    [TestFixture]
    public class ConferenceTenantControllerFixture {
        private RouteCollection _routes;
        private RouteData _routeData;
        private Mock<IConferenceDao> _dao;
        private TestController _sut;

        [SetUp]
        public void Setup() {
            this._routes = new RouteCollection();

            this._routeData = new RouteData();
            this._routeData.Values.Add("controller", "Test");
            this._routeData.Values.Add("conferenceCode", "demo");

            var requestMock = new Mock<HttpRequestBase>(MockBehavior.Strict);
            requestMock.SetupGet(x => x.ApplicationPath).Returns("/");
            requestMock.SetupGet(x => x.Url).Returns(new Uri("http://localhost/request", UriKind.Absolute));
            requestMock.SetupGet(x => x.ServerVariables).Returns(new NameValueCollection());
            requestMock.Setup(x => x.ValidateInput());

            var responseMock = new Mock<HttpResponseBase>(MockBehavior.Strict);
            responseMock.Setup(x => x.ApplyAppPathModifier(It.IsAny<string>())).Returns<string>(s => s);

            var context =
                Mock.Of<HttpContextBase>(c => c.Request == requestMock.Object && c.Response == responseMock.Object);

            this._dao = new Mock<IConferenceDao>();

            this._sut = new TestController(this._dao.Object);
            this._sut.ControllerContext = new ControllerContext(context, this._routeData, this._sut);
            this._sut.Url = new UrlHelper(new RequestContext(context, this._routeData), this._routes);
        }

        [Test]
        public void When_rendering_view_then_seats_conference_view_model() {
            var dto = new ConferenceAlias();
            this._dao.Setup(x => x.GetConferenceAlias("demo")).Returns(dto);

            var invoker = new Mock<ControllerActionInvoker> {CallBase = true};
            invoker.Protected().Setup("InvokeActionResult", ItExpr.IsAny<ControllerContext>(),
                ItExpr.IsAny<ActionResult>());

            this._routeData.Values.Add("action", "Display");
            var result = invoker.Object.InvokeAction(this._sut.ControllerContext, "Display");

            Assert.IsTrue(result);
            Assert.NotNull((object)this._sut.ViewBag.Conference);
            Assert.AreSame(dto, this._sut.ViewBag.Conference);
        }

        [Test]
        public void When_result_is_not_view_then_does_not_set_viewbag() {
            var invoker = new Mock<ControllerActionInvoker> {CallBase = true};
            invoker.Protected().Setup("InvokeActionResult", ItExpr.IsAny<ControllerContext>(),
                ItExpr.IsAny<ActionResult>());

            this._routeData.Values.Add("action", "Redirect");
            var result = invoker.Object.InvokeAction(this._sut.ControllerContext, "Redirect");

            Assert.IsTrue(result);
            Assert.IsNull((object)this._sut.ViewBag.Conference);
        }

        [Test]
        public void When_invalid_conference_code_then_http_not_found() {
            var invoker = new Mock<ControllerActionInvoker> {CallBase = true};
            ActionResult result = null;
            invoker.Protected().Setup("InvokeActionResult", ItExpr.IsAny<ControllerContext>(),
                ItExpr.IsAny<ActionResult>())
                .Callback<ControllerContext, ActionResult>((c, r) => result = r);

            this._routeData.Values.Add("action", "Display");
            invoker.Object.InvokeAction(this._sut.ControllerContext, "Display");

            Assert.NotNull(result);
            Assert.IsTrue(result is HttpNotFoundResult);
        }
        
    }

    public class TestController: ConferenceTenantController {
        public TestController(IConferenceDao conferenceDao): base(conferenceDao) {}

        public ActionResult Display() {
            return this.View();
        }

        public ActionResult Redirect() {
            return Redirect("contoso.com");
        }
    }
}