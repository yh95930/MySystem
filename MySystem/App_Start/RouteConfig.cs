﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace FineUIMvc.EmptyProject
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{TableId}/{MajorId}",
                defaults: new { controller = "Home", action = "Index", TableId = UrlParameter.Optional, MajorId = UrlParameter.Optional }
            );
        }
    }
}
