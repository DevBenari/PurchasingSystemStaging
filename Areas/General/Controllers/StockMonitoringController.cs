using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PurchasingSystemStaging.Areas.MasterData.Repositories;
using PurchasingSystemStaging.Areas.Order.Repositories;
using PurchasingSystemStaging.Controllers;
using PurchasingSystemStaging.Data;
using PurchasingSystemStaging.Hubs;
using PurchasingSystemStaging.Models;

namespace PurchasingSystemStaging.Areas.General.Controllers
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
