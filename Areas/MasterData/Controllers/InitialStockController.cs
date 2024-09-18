using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NuGet.Configuration;
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
    public class InitialStockController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IInitialStockRepository _initialStockRepository;
        private readonly IProductRepository _productRepository;
        private readonly IPrincipalRepository _principalRepository;
        private readonly ILeadTimeRepository _leadTimeRepository;

        private readonly IHostingEnvironment _hostingEnvironment;

        public InitialStockController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IInitialStockRepository InitialStockRepository,
            IUserActiveRepository userActiveRepository,
            IProductRepository productRepository,
            IPrincipalRepository principalRepository,
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
            _principalRepository = principalRepository;
            _leadTimeRepository = leadTimeRepository;

            _hostingEnvironment = hostingEnvironment;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            ViewBag.Active = "MasterData";
            var data = _initialStockRepository.GetAllInitialStock();
            return View(data);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Index(DateTime tglAwalPencarian, DateTime tglAkhirPencarian)
        {
            ViewBag.Active = "MasterData";
            ViewBag.tglAwalPencarian = tglAwalPencarian.ToString("dd MMMM yyyy");
            ViewBag.tglAkhirPencarian = tglAkhirPencarian.ToString("dd MMMM yyyy");

            var data = _initialStockRepository.GetAllInitialStock().Where(r => r.CreateDateTime.Date >= tglAwalPencarian && r.CreateDateTime.Date <= tglAkhirPencarian).ToList();
            return View(data);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ViewResult> CreateInitialStock()
        {
            ViewBag.Active = "MasterData";

            ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
            ViewBag.Principal = new SelectList(await _principalRepository.GetPrincipals(), "PrincipalId", "PrincipalName", SortOrder.Ascending);
            ViewBag.LeadTime = new SelectList(await _leadTimeRepository.GetLeadTimes(), "LeadTimeId", "LeadTimeValue", SortOrder.Ascending);

            var initialStock = new InitialStockViewModel();       

            return View(initialStock);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateInitialStock(InitialStockViewModel vm)
        {            
            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            var getProduct = _productRepository.GetAllProduct().Where(p => p.ProductId == vm.ProductId).FirstOrDefault();
            var getPrincipal = _principalRepository.GetAllPrincipal().Where(p => p.PrincipalId == vm.PrincipalId).FirstOrDefault();

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
                    PrincipalId = vm.PrincipalId,
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
                    product.BufferStock = (vm.MaxRequest - vm.AverageRequest) * leadtime.LeadTimeValue;
                    product.MinStock = (vm.AverageRequest * leadtime.LeadTimeValue) + product.BufferStock;
                    product.MaxStock = 2 * (vm.AverageRequest * leadtime.LeadTimeValue) + product.BufferStock;

                    _applicationDbContext.Entry(product).State = EntityState.Modified;
                }
                else
                {
                    TempData["WarningMessage"] = "Data not found !!!";
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
            else if (vm.PrincipalId != null)
            {
                var initialStock = new InitialStock
                {
                    CreateDateTime = DateTime.Now,
                    CreateBy = new Guid(getUser.Id),
                    GenerateBy = vm.GenerateBy,
                    InitialStockId = vm.InitialStockId,
                    ProductId = vm.ProductId,
                    PrincipalId = vm.PrincipalId,
                    PrincipalName = getPrincipal.PrincipalName,
                    LeadTimeId = vm.LeadTimeId,
                    CalculateBaseOn = vm.CalculateBaseOn,
                    MaxRequest = vm.MaxRequest,
                    AverageRequest = vm.AverageRequest
                };

                var leadtime = _leadTimeRepository.GetAllLeadTime().Where(l => l.LeadTimeId == vm.LeadTimeId).FirstOrDefault();
                var productPrincipal = _productRepository.GetAllProduct().Where(p => p.PrincipalId == vm.PrincipalId).ToList();                

                foreach (var data in productPrincipal)
                {
                    var product = _productRepository.GetAllProduct().Where(p => p.ProductId == data.ProductId).FirstOrDefault();

                    if (product != null)
                    {
                        product.UpdateDateTime = DateTime.Now;
                        product.UpdateBy = new Guid(getUser.Id);
                        product.BufferStock = (vm.MaxRequest - vm.AverageRequest) * leadtime.LeadTimeValue;
                        product.MinStock = (vm.AverageRequest * leadtime.LeadTimeValue) + product.BufferStock;
                        product.MaxStock = 2 * (vm.AverageRequest * leadtime.LeadTimeValue) + product.BufferStock;

                        _applicationDbContext.Entry(product).State = EntityState.Modified;
                        _applicationDbContext.SaveChanges();
                    }
                    else
                    {
                        TempData["WarningMessage"] = "Data not found !!!";
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
                    return View(vm);
                }
            }
            else {
                ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
                ViewBag.Principal = new SelectList(await _principalRepository.GetPrincipals(), "PrincipalId", "PrincipalName", SortOrder.Ascending);
                ViewBag.LeadTime = new SelectList(await _leadTimeRepository.GetLeadTimes(), "LeadTimeId", "LeadTimeValue", SortOrder.Ascending);
                TempData["WarningMessage"] = "Please, select product or principal !!!";
                return View(vm);
            }            
        }        
    }
}
