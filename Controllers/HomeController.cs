using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using PurchasingSystemApps.Areas.MasterData.Repositories;
using PurchasingSystemApps.Areas.MasterData.ViewModels;
using PurchasingSystemApps.Areas.Order.Models;
using PurchasingSystemApps.Areas.Order.Repositories;
using PurchasingSystemApps.Data;
using PurchasingSystemApps.Hubs;
using PurchasingSystemApps.Models;
using PurchasingSystemApps.Repositories;
using PurchasingSystemApps.ViewModels;
using System.Diagnostics;
using System.Security.Claims;

namespace PurchasingSystemApps.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IPurchaseRequestRepository _purchaseRequestRepository;

        public HomeController(ILogger<HomeController> logger,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IHubContext<ChatHub> hubContext,
            IPurchaseRequestRepository purchaseRequestRepository,
            IUserActiveRepository userActiveRepository)
        {
            _logger = logger;
            _applicationDbContext = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _hubContext = hubContext;
            _userActiveRepository = userActiveRepository;
            _purchaseRequestRepository = purchaseRequestRepository;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Active = "Dashboard";
            var countUser = _applicationDbContext.UserActives.GroupBy(u => u.UserActiveId).Select(y => new
            {
                UserActiveId = y.Key,
                CountOfUsers = y.Count()
            }).ToList();
            ViewBag.CountUser = countUser.Count;

            var countSupplier = _applicationDbContext.Suppliers.GroupBy(u => u.SupplierId).Select(y => new
            {
                SupplierId = y.Key,
                CountOfSuppliers = y.Count()
            }).ToList();
            ViewBag.CountSupplier = countSupplier.Count;

            var countProduct = _applicationDbContext.Products.GroupBy(u => u.ProductId).Select(y => new
            {
                ProductId = y.Key,
                CountOfProducts = y.Count()
            }).ToList();
            ViewBag.CountProduct = countProduct.Count;

            var countPurchaseRequest = _applicationDbContext.PurchaseRequests.GroupBy(u => u.PurchaseRequestId).Select(y => new
            {
                PurchaseRequestId = y.Key,
                CountOfPurchaseRequests = y.Count()
            }).ToList();
            ViewBag.CountPurchaseRequest = countPurchaseRequest.Count;

            var countPurchaseOrder = _applicationDbContext.PurchaseOrders.GroupBy(u => u.PurchaseOrderId).Select(y => new
            {
                PurchaseOrderId = y.Key,
                CountOfPurchaseOrders = y.Count()
            }).ToList();
            ViewBag.CountPurchaseOrder = countPurchaseOrder.Count;

            var countReceiveOrder = _applicationDbContext.ReceiveOrders.GroupBy(u => u.ReceiveOrderId).Select(y => new
            {
                ReceiveOrderId = y.Key,
                CountOfReceiveOrders = y.Count()
            }).ToList();
            ViewBag.CountReceiveOrder = countReceiveOrder.Count;

            var checkUserLogin = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            var user = _userActiveRepository.GetAllUser().Where(u => u.FullName == checkUserLogin.NamaUser).FirstOrDefault();

            //Signal R

            var data2 = _purchaseRequestRepository.GetAllPurchaseRequest();
            //var data2 = new List<dynamic>
            //        {
            //            new { CreateBy = "USER1001USER1001USER1001", PurchaseRequestNumber = "PR001", CreateDateTime = DateTime.Parse("2024-09-26 10:00:00") },
            //            new { CreateBy = "USER1002", PurchaseRequestNumber = "PR002", CreateDateTime = DateTime.Parse("2024-09-20 09:00:00") },
            //            new { CreateBy = "USER1003", PurchaseRequestNumber = "PR003", CreateDateTime = DateTime.Parse("2024-09-15 08:30:00") }
            //        };

            var loggerData = new List<string>();

            foreach (var logger in data2)
            {
                var detail = $"{logger.CreateBy}, {logger.PurchaseRequestNumber}, {logger.CreateDateTime}";
                loggerData.Add(detail);
            }

            int totalKaryawan = data2.Count();
            var loggerDataJson = JsonConvert.SerializeObject(loggerData);
            ViewBag.TotalKaryawan = totalKaryawan;
            ViewBag.LoggerData = loggerDataJson;
            await _hubContext.Clients.All.SendAsync("UpdateDataCount", totalKaryawan);
            await _hubContext.Clients.All.SendAsync("UpdateDataLogger", loggerDataJson);

            //End Signal R

            if (user != null) {
                UserActiveViewModel viewModel = new UserActiveViewModel
                {
                    UserActiveId = user.UserActiveId,
                    UserActiveCode = user.UserActiveCode,
                    FullName = user.FullName,
                    IdentityNumber = user.IdentityNumber,
                    DepartmentId = user.DepartmentId,
                    Department = user.Department.DepartmentName,
                    PositionId = user.PositionId,
                    Position = user.Position.PositionName,
                    PlaceOfBirth = user.PlaceOfBirth,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    Address = user.Address,
                    Handphone = user.Handphone,
                    Email = user.Email,
                    UserPhotoPath = user.Foto
                };
                return View(viewModel);
            } else if (user == null && checkUserLogin.NamaUser == "SuperAdmin")
            {
                UserActiveViewModel viewModel = new UserActiveViewModel
                {
                    UserActiveCode = checkUserLogin.KodeUser,
                    FullName = checkUserLogin.NamaUser,
                    Email = checkUserLogin.Email
                };
                return View(viewModel);
            }
            return View();
        }

        public IActionResult MyProfile()
        {
            ViewBag.Active = "Dashboard";
            var countUser = _applicationDbContext.UserActives.GroupBy(u => u.UserActiveId).Select(y => new
            {
                UserActiveId = y.Key,
                CountOfUsers = y.Count()
            }).ToList();
            ViewBag.CountUser = countUser.Count;

            var countSupplier = _applicationDbContext.Suppliers.GroupBy(u => u.SupplierId).Select(y => new
            {
                SupplierId = y.Key,
                CountOfSuppliers = y.Count()
            }).ToList();
            ViewBag.CountSupplier = countSupplier.Count;

            var countProduct = _applicationDbContext.Products.GroupBy(u => u.ProductId).Select(y => new
            {
                ProductId = y.Key,
                CountOfProducts = y.Count()
            }).ToList();
            ViewBag.CountProduct = countProduct.Count;

            var countPurchaseRequest = _applicationDbContext.PurchaseRequests.GroupBy(u => u.PurchaseRequestId).Select(y => new
            {
                PurchaseRequestId = y.Key,
                CountOfPurchaseRequests = y.Count()
            }).ToList();
            ViewBag.CountPurchaseRequest = countPurchaseRequest.Count;

            var countPurchaseOrder = _applicationDbContext.PurchaseOrders.GroupBy(u => u.PurchaseOrderId).Select(y => new
            {
                PurchaseOrderId = y.Key,
                CountOfPurchaseOrders = y.Count()
            }).ToList();
            ViewBag.CountPurchaseOrder = countPurchaseOrder.Count;

            var countReceiveOrder = _applicationDbContext.ReceiveOrders.GroupBy(u => u.ReceiveOrderId).Select(y => new
            {
                ReceiveOrderId = y.Key,
                CountOfReceiveOrders = y.Count()
            }).ToList();
            ViewBag.CountReceiveOrder = countReceiveOrder.Count;

            var checkUserLogin = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            var user = _userActiveRepository.GetAllUser().Where(u => u.FullName == checkUserLogin.NamaUser).FirstOrDefault();

            if (user != null)
            {
                UserActiveViewModel viewModel = new UserActiveViewModel
                {
                    UserActiveId = user.UserActiveId,
                    UserActiveCode = user.UserActiveCode,
                    FullName = user.FullName,
                    IdentityNumber = user.IdentityNumber,
                    DepartmentId = user.DepartmentId,
                    Department = user.Department.DepartmentName,
                    PositionId = user.PositionId,
                    Position = user.Position.PositionName,
                    PlaceOfBirth = user.PlaceOfBirth,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    Address = user.Address,
                    Handphone = user.Handphone,
                    Email = user.Email,
                    UserPhotoPath = user.Foto
                };
                return View(viewModel);
            }
            else if (user == null && checkUserLogin.NamaUser == "SuperAdmin")
            {
                UserActiveViewModel viewModel = new UserActiveViewModel
                {
                    UserActiveCode = checkUserLogin.KodeUser,
                    FullName = checkUserLogin.NamaUser,
                    Email = checkUserLogin.Email
                };
                return View(viewModel);
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]

        public async Task<IActionResult> GetProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = new Guid(user.Id);
            var data = _userActiveRepository.GetAllUser().Where(x => x.CreateBy == userId).ToList();

            //// Gunakan userId untuk melakukan sesuatu, misalnya:
            ViewBag.UserId = userId;
            return Json(data);
        }
        public async Task<IActionResult> Logout()
        {
            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            var user = await _signInManager.UserManager.FindByNameAsync(getUser.Email);

            if (user != null)
            {
                user.IsOnline = false;
                await _userManager.UpdateAsync(user);
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
