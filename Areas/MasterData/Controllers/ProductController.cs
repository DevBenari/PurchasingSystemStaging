using FastReport.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PurchasingSystemStaging.Areas.MasterData.Models;
using PurchasingSystemStaging.Areas.MasterData.Repositories;
using PurchasingSystemStaging.Areas.MasterData.ViewModels;
using PurchasingSystemStaging.Data;
using PurchasingSystemStaging.Models;
using PurchasingSystemStaging.Repositories;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Web.WebPages;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace PurchasingSystemStaging.Areas.MasterData.Controllers
{
    [Area("MasterData")]
    [Route("MasterData/[Controller]/[Action]")]
    public class ProductController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IProductRepository _productRepository;

        private readonly ISupplierRepository _SupplierRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMeasurementRepository _measurementRepository;
        private readonly IDiscountRepository _discountRepository;
        private readonly IWarehouseLocationRepository _warehouseLocationRepository;
        private readonly HttpClient _httpClient;
        private readonly IDataProtector _protector;

        private readonly IHostingEnvironment _hostingEnvironment;

        public ProductController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IProductRepository productRepository,
            IUserActiveRepository userActiveRepository,
            
            ISupplierRepository SupplierRepository,
            ICategoryRepository categoryRepository,
            IMeasurementRepository measurementRepository,
            IDiscountRepository discountRepository,
            IWarehouseLocationRepository warehouseLocationRepository,
            HttpClient httpClient,
            IDataProtectionProvider provider,

            IHostingEnvironment hostingEnvironment
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _productRepository = productRepository;
            _userActiveRepository = userActiveRepository;

            _SupplierRepository = SupplierRepository;
            _categoryRepository = categoryRepository;
            _measurementRepository = measurementRepository;
            _discountRepository = discountRepository;
            _warehouseLocationRepository = warehouseLocationRepository;
            _httpClient = httpClient;
            _protector = provider.CreateProtector("UrlProtector");

            _hostingEnvironment = hostingEnvironment;
        }

        public string CompressAndEncrypt(string input)
        {
            // Kompresi data
            byte[] inputData = Encoding.UTF8.GetBytes(input);
            using var compressedStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                gzipStream.Write(inputData, 0, inputData.Length);
            }

            byte[] compressedData = compressedStream.ToArray();

            // Enkripsi data
            byte[] encryptedData = _protector.Protect(compressedData);

            // Base62 encode untuk URL-safe hasil
            return Base62Encoder.Encode(encryptedData);
        }

        public IActionResult RedirectToIndex(string filterOptions = "", string searchTerm = "", DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, int page = 1, int pageSize = 10)
        {
            // Format tanggal tanpa waktu
            string startDateString = startDate.HasValue ? startDate.Value.ToString("yyyy-MM-dd") : "";
            string endDateString = endDate.HasValue ? endDate.Value.ToString("yyyy-MM-dd") : "";

            // Bangun originalPath dengan format tanggal ISO 8601
            string originalPath = $"Page:MasterData/Product/Index?filterOptions={filterOptions}&searchTerm={searchTerm}&startDate={startDateString}&endDate={endDateString}&page={page}&pageSize={pageSize}";
            string encryptedPath = CompressAndEncrypt(originalPath);

            // Redirect ke URL pendek
            return Redirect("/" + encryptedPath);
        }

        [HttpGet]
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
                (startDate, endDate) = GetDateRange(filterOptions);
            }

            var data = await _productRepository.GetAllProductPageSize(searchTerm, page, pageSize, startDate, endDate);

            var model = new Pagination<Product>
            {
                Items = data.products,
                TotalCount = data.totalCountProducts,
                PageSize = pageSize,
                CurrentPage = page,
            };

            return View(model);
        }

        private (DateTimeOffset?, DateTimeOffset?) GetDateRange(string filterOptions)
        {
            var now = DateTimeOffset.Now;
            return filterOptions switch
            {
                "Today" => (now.Date, now.Date),
                "Last Day" => (now.AddDays(-1).Date, now.AddDays(-1).Date),
                "Last 7 Days" => (now.AddDays(-7).Date, now.Date),
                "Last 30 Days" => (now.AddDays(-30).Date, now.Date),
                "This Month" => (new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset), now.Date),
                "Last Month" =>
                    (new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset).AddMonths(-1),
                     new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset).AddDays(-1)),
                _ => (null, null),
            };
        }

        //[HttpPost]
        //public async Task<IActionResult> Index(DateTime? tglAwalPencarian, DateTime? tglAkhirPencarian, string filterOptions)
        //{
        //    ViewBag.Active = "MasterData";

        //    var data = _productRepository.GetAllProduct();

        //    if(tglAwalPencarian.HasValue && tglAkhirPencarian.HasValue)
        //    {
        //        data = data.Where(u => u.CreateDateTime.Date >= tglAwalPencarian.Value.Date &&
        //                               u.CreateDateTime.Date <= tglAkhirPencarian.Value.Date);
        //    }
        //    else if (!string.IsNullOrEmpty(filterOptions))
        //    {
        //        var today = DateTime.Today;
        //        switch (filterOptions)
        //        {
        //            case "Today":
        //                data = data.Where(u => u.CreateDateTime.Date == today);
        //                break;
        //            case "Last Day":
        //                data = data.Where(x => x.CreateDateTime.Date == today.AddDays(-1));
        //                break;

        //            case "Last 7 Days":
        //                var last7Days = today.AddDays(-7);
        //                data = data.Where(x => x.CreateDateTime.Date >= last7Days && x.CreateDateTime.Date <= today);
        //                break;

        //            case "Last 30 Days":
        //                var last30Days = today.AddDays(-30);
        //                data = data.Where(x => x.CreateDateTime.Date >= last30Days && x.CreateDateTime.Date <= today);
        //                break;

        //            case "This Month":
        //                var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
        //                data = data.Where(x => x.CreateDateTime.Date >= firstDayOfMonth && x.CreateDateTime.Date <= today);
        //                break;

        //            case "Last Month":
        //                var firstDayOfLastMonth = today.AddMonths(-1).Date.AddDays(-(today.Day - 1));
        //                var lastDayOfLastMonth = today.Date.AddDays(-today.Day);
        //                data = data.Where(x => x.CreateDateTime.Date >= firstDayOfLastMonth && x.CreateDateTime.Date <= lastDayOfLastMonth);
        //                break;
        //            default:
        //                break;
        //        }
        //    }

        //    ViewBag.tglAwalPencarian = tglAwalPencarian?.ToString("dd MMMM yyyy");
        //    ViewBag.tglAkhirPencarian = tglAkhirPencarian?.ToString("dd MMMM yyyy");
        //    ViewBag.SelectedFilter = filterOptions;

        //    return View(data);
        //}

        public IActionResult RedirectToCreate()
        {
            // Enkripsi path URL untuk "Index"
            string originalPath = $"Create:MasterData/Product/CreateProduct";
            string encryptedPath = _protector.Protect(originalPath);

            // Redirect ke URL terenkripsi
            return Redirect("/" + encryptedPath);
        }

        [HttpGet]
        public async Task<ViewResult> CreateProduct()
        {
            ViewBag.Active = "MasterData";

            ViewBag.Supplier = new SelectList(await _SupplierRepository.GetSuppliers(), "SupplierId", "SupplierName", SortOrder.Ascending);
            ViewBag.Category = new SelectList(await _categoryRepository.GetCategories(), "CategoryId", "CategoryName", SortOrder.Ascending);
            ViewBag.Measurement = new SelectList(await _measurementRepository.GetMeasurements(), "MeasurementId", "MeasurementName", SortOrder.Ascending);
            ViewBag.Discount = new SelectList(await _discountRepository.GetDiscounts(), "DiscountId", "DiscountValue", SortOrder.Ascending);
            ViewBag.Warehouse = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);

            var product = new ProductViewModel();
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _productRepository.GetAllProduct().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.ProductCode).FirstOrDefault();
            if (lastCode == null)
            {
                product.ProductCode = "PDC" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.ProductCode.Substring(3, 6);

                if (lastCodeTrim != setDateNow)
                {
                    product.ProductCode = "PDC" + setDateNow + "0001";
                }
                else
                {
                    product.ProductCode = "PDC" + setDateNow + (Convert.ToInt32(lastCode.ProductCode.Substring(9, lastCode.ProductCode.Length - 9)) + 1).ToString("D4");
                }
            }

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct(ProductViewModel vm)
        {
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _productRepository.GetAllProduct().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.ProductCode).FirstOrDefault();
            if (lastCode == null)
            {
                vm.ProductCode = "PDC" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.ProductCode.Substring(3, 6);

                if (lastCodeTrim != setDateNow)
                {
                    vm.ProductCode = "PDC" + setDateNow + "0001";
                }
                else
                {
                    vm.ProductCode = "PDC" + setDateNow + (Convert.ToInt32(lastCode.ProductCode.Substring(9, lastCode.ProductCode.Length - 9)) + 1).ToString("D4");
                }
            }

            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            if (ModelState.IsValid)
            {
                var Product = new Product
                {
                    CreateDateTime = DateTime.Now,
                    CreateBy = new Guid(getUser.Id),
                    ProductId = vm.ProductId,
                    ProductCode = vm.ProductCode,
                    ProductName = vm.ProductName,
                    SupplierId = vm.SupplierId,
                    CategoryId = vm.CategoryId,
                    MeasurementId = vm.MeasurementId,
                    DiscountId = vm.DiscountId,
                    WarehouseLocationId = vm.WarehouseLocationId,
                    MinStock = vm.MinStock,
                    MaxStock = vm.MaxStock,
                    BufferStock = vm.BufferStock,
                    Stock = vm.Stock,
                    Cogs = vm.Cogs,
                    BuyPrice = vm.BuyPrice,
                    RetailPrice = vm.RetailPrice,
                    StorageLocation = vm.StorageLocation,
                    RackNumber = vm.RackNumber,
                    Note = vm.Note
                };

                var checkDuplicate = _productRepository.GetAllProduct().Where(c => c.ProductName == vm.ProductName).ToList();

                if (checkDuplicate.Count == 0)
                {
                    var result = _productRepository.GetAllProduct().Where(c => c.ProductName == vm.ProductName).FirstOrDefault();
                    if (result == null)
                    {
                        _productRepository.Tambah(Product);
                        TempData["SuccessMessage"] = "Name " + vm.ProductName + " Saved";
                        return RedirectToAction("Index", "Product");
                    }
                    else
                    {
                        ViewBag.Supplier = new SelectList(await _SupplierRepository.GetSuppliers(), "SupplierId", "SupplierName", SortOrder.Ascending);
                        ViewBag.Category = new SelectList(await _categoryRepository.GetCategories(), "CategoryId", "CategoryName", SortOrder.Ascending);
                        ViewBag.Measurement = new SelectList(await _measurementRepository.GetMeasurements(), "MeasurementId", "MeasurementName", SortOrder.Ascending);
                        ViewBag.Discount = new SelectList(await _discountRepository.GetDiscounts(), "DiscountId", "DiscountValue", SortOrder.Ascending);
                        ViewBag.Warehouse = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);

                        TempData["WarningMessage"] = "Name " + vm.ProductName + " Already Exist !!!";
                        return View(vm);
                    }
                } 
                else 
                {
                    ViewBag.Supplier = new SelectList(await _SupplierRepository.GetSuppliers(), "SupplierId", "SupplierName", SortOrder.Ascending);
                    ViewBag.Category = new SelectList(await _categoryRepository.GetCategories(), "CategoryId", "CategoryName", SortOrder.Ascending);
                    ViewBag.Measurement = new SelectList(await _measurementRepository.GetMeasurements(), "MeasurementId", "MeasurementName", SortOrder.Ascending);
                    ViewBag.Discount = new SelectList(await _discountRepository.GetDiscounts(), "DiscountId", "DiscountValue", SortOrder.Ascending);
                    ViewBag.Warehouse = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);

                    TempData["WarningMessage"] = "Name " + vm.ProductName + " There is duplicate data !!!";
                    return View(vm);
                }
            } 
            else 
            {
                ViewBag.Supplier = new SelectList(await _SupplierRepository.GetSuppliers(), "SupplierId", "SupplierName", SortOrder.Ascending);
                ViewBag.Category = new SelectList(await _categoryRepository.GetCategories(), "CategoryId", "CategoryName", SortOrder.Ascending);
                ViewBag.Measurement = new SelectList(await _measurementRepository.GetMeasurements(), "MeasurementId", "MeasurementName", SortOrder.Ascending);
                ViewBag.Discount = new SelectList(await _discountRepository.GetDiscounts(), "DiscountId", "DiscountValue", SortOrder.Ascending);
                ViewBag.Warehouse = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);
                
                return View(vm);
            }                       
        }

        public IActionResult RedirectToDetail(Guid Id)
        {
            // Enkripsi path URL untuk "Index"
            string originalPath = $"Detail:MasterData/Product/DetailProduct/{Id}";
            string encryptedPath = _protector.Protect(originalPath);

            // Redirect ke URL terenkripsi
            return Redirect("/" + encryptedPath);
        }

        [HttpGet]
        public async Task<IActionResult> DetailProduct(Guid Id)
        {
            ViewBag.Active = "MasterData";

            ViewBag.Supplier = new SelectList(await _SupplierRepository.GetSuppliers(), "SupplierId", "SupplierName", SortOrder.Ascending);
            ViewBag.Category = new SelectList(await _categoryRepository.GetCategories(), "CategoryId", "CategoryName", SortOrder.Ascending);
            ViewBag.Measurement = new SelectList(await _measurementRepository.GetMeasurements(), "MeasurementId", "MeasurementName", SortOrder.Ascending);
            ViewBag.Discount = new SelectList(await _discountRepository.GetDiscounts(), "DiscountId", "DiscountValue", SortOrder.Ascending);
            ViewBag.Warehouse = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);

            var Product = await _productRepository.GetProductById(Id);

            if (Product == null)
            {
                Response.StatusCode = 404;
                return View("UserNotFound", Id);
            }

            ProductViewModel viewModel = new ProductViewModel
            {
                ProductId = Product.ProductId,
                ProductCode = Product.ProductCode,
                ProductName = Product.ProductName,
                SupplierId = Product.SupplierId,
                CategoryId = Product.CategoryId,
                MeasurementId = Product.MeasurementId,
                DiscountId = Product.DiscountId,
                WarehouseLocationId = Product.WarehouseLocationId,
                MinStock = Product.MinStock,
                MaxStock = Product.MaxStock,
                BufferStock = Product.BufferStock,
                Stock = Product.Stock,
                Cogs = Math.Truncate((decimal)Product.Cogs),
                BuyPrice = Math.Truncate(Product.BuyPrice),
                RetailPrice = Math.Truncate(Product.RetailPrice),
                StorageLocation = Product.StorageLocation,
                RackNumber = Product.RackNumber,
                Note = Product.Note
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DetailProduct(ProductViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var Product = await _productRepository.GetProductByIdNoTracking(viewModel.ProductId);
                var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
                var checkDuplicate = _productRepository.GetAllProduct().Where(d => d.ProductName == viewModel.ProductName && d.SupplierId == viewModel.SupplierId).ToList();

                if (checkDuplicate.Count == 0 || checkDuplicate.Count == 1)
                {
                    var data = _productRepository.GetAllProduct().Where(d => d.ProductCode == viewModel.ProductCode).FirstOrDefault();

                    if (data != null)
                    {
                        Product.UpdateDateTime = DateTime.Now;
                        Product.UpdateBy = new Guid(getUser.Id);
                        Product.ProductCode = viewModel.ProductCode;
                        Product.ProductName = viewModel.ProductName;
                        Product.SupplierId = viewModel.SupplierId;
                        Product.CategoryId = viewModel.CategoryId;
                        Product.MeasurementId = viewModel.MeasurementId;
                        Product.DiscountId = viewModel.DiscountId;
                        Product.WarehouseLocationId = viewModel.WarehouseLocationId;
                        Product.MinStock = viewModel.MinStock;
                        Product.MaxStock = viewModel.MaxStock;
                        Product.BufferStock = viewModel.BufferStock;
                        Product.Stock = viewModel.Stock;
                        Product.Cogs = viewModel.Cogs;
                        Product.BuyPrice = viewModel.BuyPrice;
                        Product.RetailPrice = viewModel.RetailPrice;
                        Product.StorageLocation = viewModel.StorageLocation;
                        Product.RackNumber = viewModel.RackNumber;
                        Product.Note = viewModel.Note;

                        _productRepository.Update(Product);
                        _applicationDbContext.SaveChanges();

                        TempData["SuccessMessage"] = "Name " + viewModel.ProductName + " Success Changes";
                        return RedirectToAction("Index", "Product");
                    }
                    else
                    {
                        ViewBag.Supplier = new SelectList(await _SupplierRepository.GetSuppliers(), "SupplierId", "SupplierName", SortOrder.Ascending);
                        ViewBag.Category = new SelectList(await _categoryRepository.GetCategories(), "CategoryId", "CategoryName", SortOrder.Ascending);
                        ViewBag.Measurement = new SelectList(await _measurementRepository.GetMeasurements(), "MeasurementId", "MeasurementName", SortOrder.Ascending);
                        ViewBag.Discount = new SelectList(await _discountRepository.GetDiscounts(), "DiscountId", "DiscountValue", SortOrder.Ascending);
                        ViewBag.Warehouse = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);
                        TempData["WarningMessage"] = "Name " + viewModel.ProductName + " Already Exist !!!";
                        return View(viewModel);
                    }
                } 
                else
                {
                    ViewBag.Supplier = new SelectList(await _SupplierRepository.GetSuppliers(), "SupplierId", "SupplierName", SortOrder.Ascending);
                    ViewBag.Category = new SelectList(await _categoryRepository.GetCategories(), "CategoryId", "CategoryName", SortOrder.Ascending);
                    ViewBag.Measurement = new SelectList(await _measurementRepository.GetMeasurements(), "MeasurementId", "MeasurementName", SortOrder.Ascending);
                    ViewBag.Discount = new SelectList(await _discountRepository.GetDiscounts(), "DiscountId", "DiscountValue", SortOrder.Ascending);
                    ViewBag.Warehouse = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);
                    TempData["WarningMessage"] = "Name " + viewModel.ProductName + " There is duplicate data !!!";
                    return View(viewModel);
                }
            }
            ViewBag.Supplier = new SelectList(await _SupplierRepository.GetSuppliers(), "SupplierId", "SupplierName", SortOrder.Ascending);
            ViewBag.Category = new SelectList(await _categoryRepository.GetCategories(), "CategoryId", "CategoryName", SortOrder.Ascending);
            ViewBag.Measurement = new SelectList(await _measurementRepository.GetMeasurements(), "MeasurementId", "MeasurementName", SortOrder.Ascending);
            ViewBag.Discount = new SelectList(await _discountRepository.GetDiscounts(), "DiscountId", "DiscountValue", SortOrder.Ascending);
            ViewBag.Warehouse = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);
            return View(viewModel);
        }

        public IActionResult RedirectToDelete(Guid Id)
        {
            // Enkripsi path URL untuk "Index"
            string originalPath = $"MasterData/Product/DeleteProduct/{Id}";
            string encryptedPath = _protector.Protect(originalPath);

            // Redirect ke URL terenkripsi
            return Redirect("/" + encryptedPath);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteProduct(Guid Id)
        {
            ViewBag.Active = "MasterData";
            var Product = await _productRepository.GetProductById(Id);
            if (Product == null)
            {
                Response.StatusCode = 404;
                return View("ProductNotFound", Id);
            }

            ProductViewModel vm = new ProductViewModel
            {
                ProductId = Product.ProductId,
                ProductCode = Product.ProductCode,
                ProductName = Product.ProductName,
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(ProductViewModel vm)
        {
            //Hapus Data
            var Product = _applicationDbContext.Products.FirstOrDefault(x => x.ProductId == vm.ProductId);
            _applicationDbContext.Attach(Product);
            _applicationDbContext.Entry(Product).State = EntityState.Deleted;
            _applicationDbContext.SaveChanges();

            TempData["SuccessMessage"] = "Name " + vm.ProductName + " Success Deleted";
            return RedirectToAction("Index", "Product");
        }
    }
}
