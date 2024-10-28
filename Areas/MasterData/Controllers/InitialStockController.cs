﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NuGet.Configuration;
using PurchasingSystemStaging.Areas.MasterData.Models;
using PurchasingSystemStaging.Areas.MasterData.Repositories;
using PurchasingSystemStaging.Areas.MasterData.ViewModels;
using PurchasingSystemStaging.Data;
using PurchasingSystemStaging.Models;
using PurchasingSystemStaging.Repositories;
using System.Windows.Forms;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace PurchasingSystemStaging.Areas.MasterData.Controllers
{
    [Area("MasterData")]
    [Route("MasterData/[Controller]/[Action]")]
    public class InitialStockController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IInitialStockRepository _initialStockRepository;
        private readonly IProductRepository _productRepository;
        private readonly ISupplierRepository _SupplierRepository;
        private readonly ILeadTimeRepository _leadTimeRepository;

        private readonly IHostingEnvironment _hostingEnvironment;

        public InitialStockController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IInitialStockRepository InitialStockRepository,
            IUserActiveRepository userActiveRepository,
            IProductRepository productRepository,
            ISupplierRepository SupplierRepository,
            ILeadTimeRepository leadTimeRepository,

            IHostingEnvironment hostingEnvironment
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _initialStockRepository = InitialStockRepository;
            _userActiveRepository = userActiveRepository;
            _productRepository = productRepository;
            _SupplierRepository = SupplierRepository;
            _leadTimeRepository = leadTimeRepository;

            _hostingEnvironment = hostingEnvironment;
        }

        //public JsonResult LoadLeadTime(Guid Id)
        //{
        //    var leadtime = _applicationDbContext.Suppliers.Where(p => p.LeadTimeId == Id).ToList();
        //    return Json(new SelectList(leadtime, "LeadTimeId", "LeadTimeValue"));
        //}

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.Active = "MasterData";
            var data = _initialStockRepository.GetAllInitialStock();
            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> Index(DateTime tglAwalPencarian, DateTime tglAkhirPencarian)
        {
            ViewBag.Active = "MasterData";
            ViewBag.tglAwalPencarian = tglAwalPencarian.ToString("dd MMMM yyyy");
            ViewBag.tglAkhirPencarian = tglAkhirPencarian.ToString("dd MMMM yyyy");

            var data = _initialStockRepository.GetAllInitialStock().Where(r => r.CreateDateTime.Date >= tglAwalPencarian && r.CreateDateTime.Date <= tglAkhirPencarian).ToList();
            return View(data);
        }

        [HttpGet]
        public async Task<ViewResult> CreateInitialStock()
        {
            ViewBag.Active = "MasterData";

            ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
            ViewBag.Supplier = new SelectList(await _SupplierRepository.GetSuppliers(), "SupplierId", "SupplierName", SortOrder.Ascending);
            ViewBag.LeadTime = new SelectList(await _leadTimeRepository.GetLeadTimes(), "LeadTimeId", "LeadTimeValue", SortOrder.Ascending);

            var initialStock = new InitialStockViewModel();       

            return View(initialStock);
        }

        [HttpPost]
        public async Task<IActionResult> CreateInitialStock(InitialStockViewModel vm)
        {            
            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            var getProduct = _productRepository.GetAllProduct().Where(p => p.ProductId == vm.ProductId).FirstOrDefault();
            var getSupplier = _SupplierRepository.GetAllSupplier().Where(p => p.SupplierId == vm.SupplierId).FirstOrDefault();

            if (vm.ProductId != null)
            {
                var initialStock = new InitialStock
                {
                    CreateDateTime = DateTime.Now,
                    CreateBy = new Guid(getUser.Id),
                    GenerateBy = vm.GenerateBy,
                    InitialStockId = vm.InitialStockId,
                    ProductId = vm.ProductId,
                    ProductName = getProduct.ProductName,
                    SupplierId = vm.SupplierId,
                    LeadTimeId = vm.LeadTimeId,
                    CalculateBaseOn = vm.CalculateBaseOn,
                    MaxRequest = vm.MaxRequest,
                    AverageRequest = vm.AverageRequest
                };

                var leadtime = _leadTimeRepository.GetAllLeadTime().Where(l => l.LeadTimeId == vm.LeadTimeId).FirstOrDefault();
                var product = _productRepository.GetAllProduct().Where(p => p.ProductId == vm.ProductId).FirstOrDefault();

                if (product != null)
                {
                    product.UpdateDateTime = DateTime.Now;
                    product.UpdateBy = new Guid(getUser.Id);

                    if (vm.CalculateBaseOn == "Daily")
                    {
                        int daily = 1;

                        product.BufferStock = ((vm.MaxRequest / daily) - vm.AverageRequest) * leadtime.LeadTimeValue;

                        if (product.BufferStock < 0)
                        {
                            TempData["WarningMessage"] = "Sorry, the calculation is incorrect !!!";
                            ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
                            ViewBag.Supplier = new SelectList(await _SupplierRepository.GetSuppliers(), "SupplierId", "SupplierName", SortOrder.Ascending);
                            ViewBag.LeadTime = new SelectList(await _leadTimeRepository.GetLeadTimes(), "LeadTimeId", "LeadTimeValue", SortOrder.Ascending);
                            return View(vm);
                        }
                    }
                    else if (vm.CalculateBaseOn == "Weekly")
                    {
                        int weekly = 7;
                        product.BufferStock = ((vm.MaxRequest / weekly) - vm.AverageRequest) * leadtime.LeadTimeValue;

                        if (product.BufferStock < 0)
                        {
                            TempData["WarningMessage"] = "Sorry, the calculation is incorrect !!!";
                            ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
                            ViewBag.Supplier = new SelectList(await _SupplierRepository.GetSuppliers(), "SupplierId", "SupplierName", SortOrder.Ascending);
                            ViewBag.LeadTime = new SelectList(await _leadTimeRepository.GetLeadTimes(), "LeadTimeId", "LeadTimeValue", SortOrder.Ascending);
                            return View(vm);
                        }
                    }
                    else if (vm.CalculateBaseOn == "Monthly")
                    {
                        int monthly = 30;
                        product.BufferStock = ((vm.MaxRequest / monthly) - vm.AverageRequest) * leadtime.LeadTimeValue;

                        if (product.BufferStock < 0)
                        {
                            TempData["WarningMessage"] = "Sorry, the calculation is incorrect !!!";
                            ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
                            ViewBag.Supplier = new SelectList(await _SupplierRepository.GetSuppliers(), "SupplierId", "SupplierName", SortOrder.Ascending);
                            ViewBag.LeadTime = new SelectList(await _leadTimeRepository.GetLeadTimes(), "LeadTimeId", "LeadTimeValue", SortOrder.Ascending);
                            return View(vm);
                        }
                    }

                    product.MinStock = (vm.AverageRequest * leadtime.LeadTimeValue) + product.BufferStock;
                    product.MaxStock = 2 * (vm.AverageRequest * leadtime.LeadTimeValue) + product.BufferStock;

                    _applicationDbContext.Entry(product).State = EntityState.Modified;
                }
                else
                {
                    TempData["WarningMessage"] = "Data not found !!!";
                    ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
                    ViewBag.Supplier = new SelectList(await _SupplierRepository.GetSuppliers(), "SupplierId", "SupplierName", SortOrder.Ascending);
                    ViewBag.LeadTime = new SelectList(await _leadTimeRepository.GetLeadTimes(), "LeadTimeId", "LeadTimeValue", SortOrder.Ascending);
                    return View(vm);
                }

                var result = _initialStockRepository.GetAllInitialStock().Where(c => c.InitialStockId == vm.InitialStockId).FirstOrDefault();
                if (result == null)
                {
                    _initialStockRepository.Tambah(initialStock);
                    TempData["SuccessMessage"] = "Data Saved";
                    return RedirectToAction("Index", "InitialStock");
                }
                else
                {
                    TempData["WarningMessage"] = "Data already Exist !!!";
                    return View(vm);
                }
            }
            else if (vm.SupplierId != null)
            {
                var initialStock = new InitialStock
                {
                    CreateDateTime = DateTime.Now,
                    CreateBy = new Guid(getUser.Id),
                    GenerateBy = vm.GenerateBy,
                    InitialStockId = vm.InitialStockId,
                    ProductId = vm.ProductId,
                    SupplierId = vm.SupplierId,
                    SupplierName = getSupplier.SupplierName,
                    LeadTimeId = vm.LeadTimeId,
                    CalculateBaseOn = vm.CalculateBaseOn,
                    MaxRequest = vm.MaxRequest,
                    AverageRequest = vm.AverageRequest
                };

                var leadtime = _leadTimeRepository.GetAllLeadTime().Where(l => l.LeadTimeId == vm.LeadTimeId).FirstOrDefault();
                var productSupplier = _productRepository.GetAllProduct().Where(p => p.SupplierId == vm.SupplierId).ToList();                

                foreach (var data in productSupplier)
                {
                    var product = _productRepository.GetAllProduct().Where(p => p.ProductId == data.ProductId).FirstOrDefault();

                    if (product != null)
                    {
                        if (vm.CalculateBaseOn == "Daily")
                        {
                            int daily = 1;

                            product.BufferStock = ((vm.MaxRequest / daily) - vm.AverageRequest) * leadtime.LeadTimeValue;

                            if (product.BufferStock < 0)
                            {
                                TempData["WarningMessage"] = "Sorry, the calculation is incorrect !!!";
                                ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
                                ViewBag.Supplier = new SelectList(await _SupplierRepository.GetSuppliers(), "SupplierId", "SupplierName", SortOrder.Ascending);
                                ViewBag.LeadTime = new SelectList(await _leadTimeRepository.GetLeadTimes(), "LeadTimeId", "LeadTimeValue", SortOrder.Ascending);
                                return View(vm);
                            }
                        }
                        else if (vm.CalculateBaseOn == "Weekly")
                        {
                            int weekly = 7;
                            product.BufferStock = ((vm.MaxRequest / weekly) - vm.AverageRequest) * leadtime.LeadTimeValue;

                            if (product.BufferStock < 0)
                            {
                                TempData["WarningMessage"] = "Sorry, the calculation is incorrect !!!";
                                ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
                                ViewBag.Supplier = new SelectList(await _SupplierRepository.GetSuppliers(), "SupplierId", "SupplierName", SortOrder.Ascending);
                                ViewBag.LeadTime = new SelectList(await _leadTimeRepository.GetLeadTimes(), "LeadTimeId", "LeadTimeValue", SortOrder.Ascending);
                                return View(vm);
                            }
                        }
                        else if (vm.CalculateBaseOn == "Monthly")
                        {
                            int monthly = 30;
                            product.BufferStock = ((vm.MaxRequest / monthly) - vm.AverageRequest) * leadtime.LeadTimeValue;

                            if (product.BufferStock < 0)
                            {
                                TempData["WarningMessage"] = "Sorry, the calculation is incorrect !!!";
                                ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
                                ViewBag.Supplier = new SelectList(await _SupplierRepository.GetSuppliers(), "SupplierId", "SupplierName", SortOrder.Ascending);
                                ViewBag.LeadTime = new SelectList(await _leadTimeRepository.GetLeadTimes(), "LeadTimeId", "LeadTimeValue", SortOrder.Ascending);
                                return View(vm);
                            }
                        }

                        product.MinStock = (vm.AverageRequest * leadtime.LeadTimeValue) + product.BufferStock;
                        product.MaxStock = 2 * (vm.AverageRequest * leadtime.LeadTimeValue) + product.BufferStock;

                        _applicationDbContext.Entry(product).State = EntityState.Modified;
                        _applicationDbContext.SaveChanges();
                    }
                    else
                    {
                        TempData["WarningMessage"] = "Data not found !!!";
                        ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
                        ViewBag.Supplier = new SelectList(await _SupplierRepository.GetSuppliers(), "SupplierId", "SupplierName", SortOrder.Ascending);
                        ViewBag.LeadTime = new SelectList(await _leadTimeRepository.GetLeadTimes(), "LeadTimeId", "LeadTimeValue", SortOrder.Ascending);
                        return View(vm);
                    }                    
                }                

                var result = _initialStockRepository.GetAllInitialStock().Where(c => c.InitialStockId == vm.InitialStockId).FirstOrDefault();
                if (result == null)
                {
                    _initialStockRepository.Tambah(initialStock);
                    TempData["SuccessMessage"] = "Data Saved";
                    return RedirectToAction("Index", "InitialStock");
                }
                else
                {
                    TempData["WarningMessage"] = "Data already Exist !!!";
                    ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
                    ViewBag.Supplier = new SelectList(await _SupplierRepository.GetSuppliers(), "SupplierId", "SupplierName", SortOrder.Ascending);
                    ViewBag.LeadTime = new SelectList(await _leadTimeRepository.GetLeadTimes(), "LeadTimeId", "LeadTimeValue", SortOrder.Ascending);
                    return View(vm);
                }
            }
            else {
                ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
                ViewBag.Supplier = new SelectList(await _SupplierRepository.GetSuppliers(), "SupplierId", "SupplierName", SortOrder.Ascending);
                ViewBag.LeadTime = new SelectList(await _leadTimeRepository.GetLeadTimes(), "LeadTimeId", "LeadTimeValue", SortOrder.Ascending);
                TempData["WarningMessage"] = "Please, select product or Supplier !!!";
                return View(vm);
            }            
        }        
    }
}
