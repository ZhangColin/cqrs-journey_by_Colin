using System.Web.Mvc;

namespace Conference.Web.Public.Areas.ThirdPartyProcessor {
    public class ThirdPartyProcessorAreaRegistration : AreaRegistration {
        public override string AreaName {
            get {
                return "ThirdPartyProcessor";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) {
            context.MapRoute(
                "Pay",
                "payment",
                new { controller = "ThirdPartyProcessorPayment", action = "Pay" });

            context.MapRoute(
                "ThirdPartyProcessor_default",
                "ThirdPartyProcessor/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}