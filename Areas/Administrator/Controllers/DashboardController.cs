using Microsoft.AspNetCore.Mvc;

namespace PurchasingSystemStaging.Areas.Administrator.Controllers
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
