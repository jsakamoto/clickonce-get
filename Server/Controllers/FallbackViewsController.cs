using Microsoft.AspNetCore.Mvc;

namespace ClickOnceGet.Server.Controllers
{
    public class FallbackViewsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
