using Microsoft.AspNetCore.Mvc;
using PurchasingSystem.Areas.MasterData.Repositories;
using PurchasingSystem.Data;

namespace PurchasingSystem.Areas.Warehouse.Controllers
{
    [Area("Warehouse")]
    [Route("Warehouse/[Controller]/[Action]")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;

        public DashboardController(
            ApplicationDbContext applicationDbContext,
            IUserActiveRepository userActiveRepository
        )
        {
            _applicationDbContext = applicationDbContext;
            _userActiveRepository = userActiveRepository;
        }

        public IActionResult Index()
        {
            ViewBag.Active = "Warehouse";

            return View();
        }
    }
}
