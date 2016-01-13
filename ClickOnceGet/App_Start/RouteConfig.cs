using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using ClickOnceGet.Controllers;

namespace ClickOnceGet
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Icon",
                url: "app/{appId}/icon/{pxSize}",
                defaults: new { controller = "Publish", action = "GetIcon", pxSize = 48 }
            );

            routes.MapRoute(
                name: "Cert",
                url: "app/{appId}/cert/{*pathInfo}",
                defaults: new { controller = "Publish", action = nameof(PublishController.GetCertificate) }
            );

            routes.MapRoute(
                name: "Detail",
                url: "app/{appId}/detail",
                defaults: new { controller = "Publish", action = "Detail" }
            );

            routes.MapRoute(
                name: "Publish",
                url: "app/{appId}/{*pathInfo}",
                defaults: new { controller = "Publish", action = "Get" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
