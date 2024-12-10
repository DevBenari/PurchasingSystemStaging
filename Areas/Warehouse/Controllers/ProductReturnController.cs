using Microsoft.AspNetCore.Mvc;

namespace PurchasingSystemStaging.Areas.Warehouse.Controllers
{
    public class ProductReturnController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
