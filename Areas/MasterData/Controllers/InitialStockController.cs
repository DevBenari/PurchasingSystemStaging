﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NuGet.Configuration;
using PurchasingSystem.Areas.MasterData.Models;
using PurchasingSystem.Areas.MasterData.Repositories;
using PurchasingSystem.Areas.MasterData.ViewModels;
using PurchasingSystem.Data;
using PurchasingSystem.Models;
using PurchasingSystem.Repositories;
using System.Security.Claims;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace PurchasingSystem.Areas.MasterData.Controllers
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

        private readonly IDataProtector _protector;
        private readonly UrlMappingService _urlMappingService;
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

            IDataProtectionProvider provider,
            UrlMappingService urlMappingService,
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

            _protector = provider.CreateProtector("UrlProtector");
            _urlMappingService = urlMappingService;
            _hostingEnvironment = hostingEnvironment;
        }

        //public JsonResult LoadLeadTime(Guid Id)
        //{
        //    var leadtime = _applicationDbContext.Suppliers.Where(p => p.LeadTimeId == Id).ToList();
        //    return Json(new SelectList(leadtime, "LeadTimeId", "LeadTimeValue"));
        //}

        public async Task<IActionResult> GetSuppliersPaged(string term, int page = 1, int pageSize = 10)
        {
            // Mulai query, include relasi Supplier
            var query = _applicationDbContext.Suppliers
                .AsQueryable();

            // Filter pencarian (jika user ketik di Select2)
            if (!string.IsNullOrWhiteSpace(term))
            {
                // Misal: filter by product name
                query = query.Where(p => p.SupplierName.Contains(term));
            }

            // Total data
            int totalCount = await query.CountAsync();

            // Paging
            query = query.OrderBy(p => p.SupplierName)
                         .Skip((page - 1) * pageSize)
                         .Take(pageSize);

            var items = await query.ToListAsync();

            var results = items.Select(p => new {
                id = p.SupplierId.ToString(),
                text = p.SupplierName
            });

            bool more = (page * pageSize) < totalCount;

            var response = new
            {
                results = results,
                pagination = new
                {
                    more = more
                }
            };

            return Ok(response);
        }

        public async Task<IActionResult> GetProductsPaged(string term, int page = 1, int pageSize = 10)
        {
            // Mulai query, include relasi Supplier
            var query = _applicationDbContext.Products
                .Include(p => p.Supplier)
                .AsQueryable();

            // Filter pencarian (jika user ketik di Select2)
            if (!string.IsNullOrWhiteSpace(term))
            {
                // Misal: filter by product name
                query = query.Where(p => p.ProductName.Contains(term) || p.Supplier.SupplierName.Contains(term));
            }

            // Total data
            int totalCount = await query.CountAsync();

            // Paging
            query = query.OrderBy(p => p.ProductName)
                         .Skip((page - 1) * pageSize)
                         .Take(pageSize);

            var items = await query.ToListAsync();

            // Hasil: di Select2 butuh { id, text }
            // "text" kita isi gabungan ProductName + '|' + SupplierName
            var results = items.Select(p => new {
                id = p.ProductId,
                text = p.ProductName + " | " + p.Supplier.SupplierName
            });

            bool more = (page * pageSize) < totalCount;

            var response = new
            {
                results = results,
                pagination = new
                {
                    more = more
                }
            };

            return Ok(response);
        }

        [Authorize(Roles = "ReadInitialStock")]
        public async Task<IActionResult> Index(string filterOptions = "", string searchTerm = "", DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, int page = 1, int pageSize = 10)
        {
            ViewBag.Active = "MasterData";
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedFilter = filterOptions;

            // Format tanggal untuk input[type="date"]
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            // Format tanggal untuk tampilan (Indonesia)
            ViewBag.StartDateReadable = startDate?.ToString("dd MMMM yyyy");
            ViewBag.EndDateReadable = endDate?.ToString("dd MMMM yyyy");

            // Normalisasi tanggal untuk mengabaikan waktu
            if (startDate.HasValue) startDate = startDate.Value.Date;
            if (endDate.HasValue) endDate = endDate.Value.Date.AddDays(1).AddTicks(-1); // Sampai akhir hari

            // Tentukan range tanggal berdasarkan filterOptions
            if (!string.IsNullOrEmpty(filterOptions))
            {
                (startDate, endDate) = GetDateRangeHelper.GetDateRange(filterOptions);
            }

            var data = await _initialStockRepository.GetAllInitialStockPageSize(searchTerm, page, pageSize, startDate, endDate);

            var model = new Pagination<InitialStock>
            {
                Items = data.initialStocks,
                TotalCount = data.totalCountInitialStocks,
                PageSize = pageSize,
                CurrentPage = page,
            };

            return View(model);
        }

        [Authorize(Roles = "CreateInitialStock")]
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
        [Authorize(Roles = "CreateInitialStock")]
        public async Task<IActionResult> CreateInitialStock(InitialStockViewModel vm)
        {            
            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.FindFirst(ClaimTypes.Name).Value).FirstOrDefault();
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
