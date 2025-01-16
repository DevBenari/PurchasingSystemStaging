using Microsoft.AspNetCore.Mvc;

namespace PurchasingSystem.Areas.Administrator.Controllers
{
    [Area("Administrator")]
    [Route("Administrator/[Controller]/[Action]")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Active = "Administrator";
            return View();
        }
    }
}
