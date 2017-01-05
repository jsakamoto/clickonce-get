using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ClickOnceGet.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public ApplicationsController AppsAPI { get; set; }

        public HomeController()
        {
            this.AppsAPI = new ApplicationsController();
        }

        [AllowAnonymous]
        public ActionResult Index()
        {
            if (Request.Url.Host != "localhost" && Request.Url.Scheme != "https")
                return Redirect(Url.AppUrl(forceSecure: true));

            var allApps = this.AppsAPI.GetApps();
            return View(allApps);
        }

        public ActionResult MyApps()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult HowToPackage()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult LearnMoreAboutCertificate()
        {
            return View();
        }
    }
}