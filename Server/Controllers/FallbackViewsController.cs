using ClickOnceGet.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClickOnceGet.Server.Controllers
{
    public class FallbackViewsController : Controller
    {
        private HttpsRedirecter HttpsRedirecter { get; }

        public FallbackViewsController(HttpsRedirecter httpsRedirecter)
        {
            HttpsRedirecter = httpsRedirecter;
        }

        public IActionResult Index()
        {
            if (this.HttpsRedirecter.ShouldRedirect(this.Request, out var actionResult)) return actionResult;
            return View();
        }
    }
}
