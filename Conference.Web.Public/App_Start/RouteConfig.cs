using System.Web.Mvc;
using System.Web.Routing;

namespace Conference.Web.Public
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("{*favicon}", new { favicon = @"(.*/)?favicon.ico(/.*)?" });

            routes.MapRoute(
                "Home",
                string.Empty,
                new {controller = "Default", action = "Index"});

            routes.MapRoute(
                "ViewConference",
                "{conferenceCode}",
                new {controller = "Conference", action = "Display"});

            routes.MapRoute(
                "RegisterStart",
                "{conferenceCode}/register",
                new { controller = "Registration", action = "StartRegistration" });

            routes.MapRoute(
                "RegisterRegistrantDetails",
                "{conferenceCode}/registrant",
                new { controller = "Registration", action = "SpecifyRegistrantAndPaymentDetails" });

            routes.MapRoute(
                "StartPayment",
                "{conferenceCode}/pay",
                new { controller = "Registration", action = "StartPayment" });

            routes.MapRoute(
                "ExpiredOrder",
                "{conferenceCode}/expired",
                new { controller = "Registration", action = "ShowExpiredOrder" });

            routes.MapRoute(
                "RegisterConfirmation",
                "{conferenceCode}/confirmation",
                new { controller = "Registration", action = "ThankYou" });

            routes.MapRoute(
                "OrderFind",
                "{conferenceCode}/order/find",
                new { controller = "Order", action = "Find" });

            routes.MapRoute(
                "AssignSeats",
                "{conferenceCode}/order/{orderId}/seats",
                new { controller = "Order", action = "AssignSeats" });

            routes.MapRoute(
                "AssignSeatsWithoutAssignmentsId",
                "{conferenceCode}/order/{orderId}/seats-redirect",
                new { controller = "Order", action = "AssignSeatsForOrder" });

            routes.MapRoute(
                "OrderDisplay",
                "{conferenceCode}/order/{orderId}",
                new { controller = "Order", action = "Display" });

            routes.MapRoute(
                "InitiateThirdPartyPayment",
                "{conferenceCode}/third-party-payment",
                new { controller = "Payment", action = "ThirdPartyProcessorPayment" });

            routes.MapRoute(
                "PaymentAccept",
                "{conferenceCode}/third-party-payment-accept",
                new { controller = "Payment", action = "ThirdPartyProcessorPaymentAccepted" });

            routes.MapRoute(
                "PaymentReject",
                "{conferenceCode}/third-party-payment-reject",
                new { controller = "Payment", action = "ThirdPartyProcessorPaymentRejected" });

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
