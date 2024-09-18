using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PurchasingSystemApps.Areas.MasterData.Models;
using PurchasingSystemApps.Areas.MasterData.Repositories;
using PurchasingSystemApps.Areas.MasterData.ViewModels;
using PurchasingSystemApps.Data;
using PurchasingSystemApps.Models;
using PurchasingSystemApps.Repositories;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace PurchasingSystemApps.Areas.MasterData.Controllers
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

        private readonly IPrincipalRepository _principalRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMeasurementRepository _measurementRepository;
        private readonly IDiscountRepository _discountRepository;
        private readonly IWarehouseLocationRepository _warehouseLocationRepository;

        private readonly IHostingEnvironment _hostingEnvironment;

        public ProductController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IProductRepository productRepository,
            IUserActiveRepository userActiveRepository,
            
            IPrincipalRepository principalRepository,
            ICategoryRepository categoryRepository,
            IMeasurementRepository measurementRepository,
            IDiscountRepository discountRepository,
            IWarehouseLocationRepository warehouseLocationRepository,

            IHostingEnvironment hostingEnvironment
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _productRepository = productRepository;
            _userActiveRepository = userActiveRepository;

            _principalRepository = principalRepository;
            _categoryRepository = categoryRepository;
            _measurementRepository = measurementRepository;
            _discountRepository = discountRepository;
            _warehouseLocationRepository = warehouseLocationRepository;

            _hostingEnvironment = hostingEnvironment;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            ViewBag.Active = "MasterData";
            var data = _productRepository.GetAllProduct();
            return View(data);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Index(DateTime tglAwalPencarian, DateTime tglAkhirPencarian)
        {
            ViewBag.Active = "MasterData";
            ViewBag.tglAwalPencarian = tglAwalPencarian.ToString("dd MMMM yyyy");
            ViewBag.tglAkhirPencarian = tglAkhirPencarian.ToString("dd MMMM yyyy");

            var data = _productRepository.GetAllProduct().Where(r => r.CreateDateTime.Date >= tglAwalPencarian && r.CreateDateTime.Date <= tglAkhirPencarian).ToList();
            return View(data);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ViewResult> CreateProduct()
        {
            ViewBag.Active = "MasterData";

            ViewBag.Principal = new SelectList(await _principalRepository.GetPrincipals(), "PrincipalId", "PrincipalName", SortOrder.Ascending);
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
        [AllowAnonymous]
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
                    PrincipalId = vm.PrincipalId,
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

                var result = _productRepository.GetAllProduct().Where(c => c.ProductName == vm.ProductName).FirstOrDefault();
                if (result == null)
                {
                    _productRepository.Tambah(Product);
                    TempData["SuccessMessage"] = "Name " + vm.ProductName + " Saved";
                    return RedirectToAction("Index", "Product");
                }
                else
                {
                    ViewBag.Principal = new SelectList(await _principalRepository.GetPrincipals(), "PrincipalId", "PrincipalName", SortOrder.Ascending);
                    ViewBag.Category = new SelectList(await _categoryRepository.GetCategories(), "CategoryId", "CategoryName", SortOrder.Ascending);
                    ViewBag.Measurement = new SelectList(await _measurementRepository.GetMeasurements(), "MeasurementId", "MeasurementName", SortOrder.Ascending);
                    ViewBag.Discount = new SelectList(await _discountRepository.GetDiscounts(), "DiscountId", "DiscountValue", SortOrder.Ascending);
                    ViewBag.Warehouse = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);

                    TempData["WarningMessage"] = "Name " + vm.ProductName + " Already Exist !!!";
                    return View(vm);
                }

            }
            ViewBag.Principal = new SelectList(await _principalRepository.GetPrincipals(), "PrincipalId", "PrincipalName", SortOrder.Ascending);
            ViewBag.Category = new SelectList(await _categoryRepository.GetCategories(), "CategoryId", "CategoryName", SortOrder.Ascending);
            ViewBag.Measurement = new SelectList(await _measurementRepository.GetMeasurements(), "MeasurementId", "MeasurementName", SortOrder.Ascending);
            ViewBag.Discount = new SelectList(await _discountRepository.GetDiscounts(), "DiscountId", "DiscountValue", SortOrder.Ascending);
            ViewBag.Warehouse = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);
            return View(vm);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> DetailProduct(Guid Id)
        {
            ViewBag.Active = "MasterData";

            ViewBag.Principal = new SelectList(await _principalRepository.GetPrincipals(), "PrincipalId", "PrincipalName", SortOrder.Ascending);
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
                PrincipalId = Product.PrincipalId,
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
        [AllowAnonymous]
        public async Task<IActionResult> DetailProduct(ProductViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var Product = await _productRepository.GetProductByIdNoTracking(viewModel.ProductId);
                var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
                var check = _productRepository.GetAllProduct().Where(d => d.ProductCode == viewModel.ProductCode).FirstOrDefault();

                if (check != null)
                {
                    Product.UpdateDateTime = DateTime.Now;
                    Product.UpdateBy = new Guid(getUser.Id);
                    Product.ProductCode = viewModel.ProductCode;
                    Product.ProductName = viewModel.ProductName;
                    Product.PrincipalId = viewModel.PrincipalId;
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
                    ViewBag.Principal = new SelectList(await _principalRepository.GetPrincipals(), "PrincipalId", "PrincipalName", SortOrder.Ascending);
                    ViewBag.Category = new SelectList(await _categoryRepository.GetCategories(), "CategoryId", "CategoryName", SortOrder.Ascending);
                    ViewBag.Measurement = new SelectList(await _measurementRepository.GetMeasurements(), "MeasurementId", "MeasurementName", SortOrder.Ascending);
                    ViewBag.Discount = new SelectList(await _discountRepository.GetDiscounts(), "DiscountId", "DiscountValue", SortOrder.Ascending);
                    ViewBag.Warehouse = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);
                    TempData["WarningMessage"] = "Name " + viewModel.ProductName + " Already Exist !!!";
                    return View(viewModel);
                }
            }
            ViewBag.Principal = new SelectList(await _principalRepository.GetPrincipals(), "PrincipalId", "PrincipalName", SortOrder.Ascending);
            ViewBag.Category = new SelectList(await _categoryRepository.GetCategories(), "CategoryId", "CategoryName", SortOrder.Ascending);
            ViewBag.Measurement = new SelectList(await _measurementRepository.GetMeasurements(), "MeasurementId", "MeasurementName", SortOrder.Ascending);
            ViewBag.Discount = new SelectList(await _discountRepository.GetDiscounts(), "DiscountId", "DiscountValue", SortOrder.Ascending);
            ViewBag.Warehouse = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);
            return View(viewModel);
        }

        [HttpGet]
        [AllowAnonymous]
        //[Authorize]
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
        [AllowAnonymous]
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
