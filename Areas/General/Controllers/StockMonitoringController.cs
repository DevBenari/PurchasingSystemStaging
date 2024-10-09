using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PurchasingSystemApps.Areas.MasterData.Repositories;
using PurchasingSystemApps.Areas.Order.Repositories;
using PurchasingSystemApps.Controllers;
using PurchasingSystemApps.Data;
using PurchasingSystemApps.Hubs;
using PurchasingSystemApps.Models;

namespace PurchasingSystemApps.Areas.General.Controllers
{
    [Area("General")]
    [Route("General/[Controller]/[Action]")]
    public class StockMonitoringController : Controller
    {
        private readonly IProductRepository _productRepository;

        public StockMonitoringController(
            IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public IActionResult Index()
        {
            ViewBag.Active = "General";

            var product = _productRepository.GetAllProduct().ToList();

            return View(product);
        }
    }
}
