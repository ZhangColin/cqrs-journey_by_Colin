using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Conference.Web.Admin {
    public class RouteConfig {
        public static void RegisterRoutes(RouteCollection routes) {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Conference.Locate",
                url: "locate",
                defaults: new { controller = "Conference", action = "Locate" });


            routes.MapRoute(
                name: "Conference.Create",
                url: "create",
                defaults: new {controller = "Conference", action = "Create"});

            routes.MapRoute(
                name: "Conference",
                url: "{slug}/{accessCode}/{action}",
                defaults: new {controller = "Conference", action = "Index"});


            routes.MapRoute(
                name: "Home",
                url: "",
                defaults: new { controller = "Home", action = "Index" }
            );
        }
    }
}
