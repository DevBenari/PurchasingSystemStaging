using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using PurchasingSystemApps.Areas.MasterData.Models;
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
using System.Net;
using System.Security.Claims;
using System.Web.Helpers;

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
        private readonly IProductRepository _productRepository;
        private readonly IApprovalRepository _approvalRepository;

        public HomeController(ILogger<HomeController> logger,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IHubContext<ChatHub> hubContext,
            IPurchaseRequestRepository purchaseRequestRepository,
            IUserActiveRepository userActiveRepository,
            IProductRepository productRepository,
            IApprovalRepository approvalRepository)
        {
            _logger = logger;
            _applicationDbContext = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _hubContext = hubContext;
            _userActiveRepository = userActiveRepository;
            _purchaseRequestRepository = purchaseRequestRepository;
            _productRepository = productRepository;
            _approvalRepository = approvalRepository;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Active = "Dashboard";

            var checkUserLogin = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            var getUserActive = _userActiveRepository.GetAllUser().Where(c => c.UserActiveCode == checkUserLogin.KodeUser).FirstOrDefault();
            var userLogin = _userActiveRepository.GetAllUserLogin().Where(u => u.IsOnline == true).ToList();
            var user = _userActiveRepository.GetAllUser().Where(u => u.FullName == checkUserLogin.NamaUser).FirstOrDefault();
            var product = _productRepository.GetAllProduct().Where(p => p.Stock < p.MinStock).ToList();

            var userOnline = _userActiveRepository.GetAllUserLogin().Where(u => u.IsOnline == true).GroupBy(u => u.Id).Select(y => new
            {
                Id = y.Key,
                CountOfUsers = y.Count()
            }).ToList();
            ViewBag.UserOnline = userOnline.Count;

            var countPurchaseRequest = _applicationDbContext.PurchaseRequests.Where(u => u.CreateBy == new Guid(checkUserLogin.Id)).GroupBy(u => u.PurchaseRequestId).Select(y => new
            {
                PurchaseRequestId = y.Key,
                CountOfPurchaseRequests = y.Count()
            }).ToList();
            ViewBag.CountPurchaseRequest = countPurchaseRequest.Count;

            var countPurchaseOrder = _applicationDbContext.PurchaseOrders.Where(u => u.CreateBy == new Guid(checkUserLogin.Id)).GroupBy(u => u.PurchaseOrderId).Select(y => new
            {
                PurchaseOrderId = y.Key,
                CountOfPurchaseOrders = y.Count()
            }).ToList();
            ViewBag.CountPurchaseOrder = countPurchaseOrder.Count;

            var countApproval = _applicationDbContext.Approvals.Where(u => u.UserApproveId == getUserActive.UserActiveId && u.Status == "Approve").ToList();
            ViewBag.CountApproval = countApproval.Count;

            var countUnitRequest = _applicationDbContext.UnitRequests.Where(u => u.CreateBy == new Guid(checkUserLogin.Id)).GroupBy(u => u.UnitRequestId).Select(y => new
            {
                UnitRequestId = y.Key,
                CountOfUnitRequests = y.Count()
            }).ToList();
            ViewBag.CountUnitRequest = countUnitRequest.Count;

            var countReceiveOrder = _applicationDbContext.ReceiveOrders.Where(u => u.CreateBy == new Guid(checkUserLogin.Id)).GroupBy(u => u.ReceiveOrderId).Select(y => new
            {
                ReceiveOrderId = y.Key,
                CountOfReceiveOrders = y.Count()
            }).ToList();
            ViewBag.CountReceiveOrder = countReceiveOrder.Count;

            var countApprovalUnitRequest = _applicationDbContext.ApprovalRequests.Where(u => u.CreateBy == new Guid(checkUserLogin.Id)).GroupBy(u => u.ApprovalRequestId).Select(y => new
            {
                ApprovalRequestId = y.Key,
                CountOfApprovalUnitRequests = y.Count()
            }).ToList();
            ViewBag.CountApprovalUnitRequest = countApprovalUnitRequest.Count;

            var countWarehouseTransfer = _applicationDbContext.WarehouseTransfers.Where(u => u.CreateBy == new Guid(checkUserLogin.Id)).GroupBy(u => u.WarehouseTransferId).Select(y => new
            {
                WarehouseTransferId = y.Key,
                CountOfWarehouseTransfers = y.Count()
            }).ToList();
            ViewBag.CountWarehouseTransfer = countWarehouseTransfer.Count;

            //Signal R

            var getUserId = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            var getUserActiveId = _userActiveRepository.GetAllUser().Where(u => u.UserActiveCode == getUserId.KodeUser).FirstOrDefault().UserActiveId;
            var data2 = _purchaseRequestRepository.GetAllPurchaseRequest()
                            .Where(p => (p.UserApprove1Id == getUserActiveId && p.ApproveStatusUser1 == null) 
                            || (p.UserApprove2Id == getUserActiveId && p.ApproveStatusUser1 == "Approve" && p.ApproveStatusUser2 == null) 
                            || (p.UserApprove3Id == getUserActiveId && p.ApproveStatusUser1 == "Approve" && p.ApproveStatusUser2 == "Approve" && p.ApproveStatusUser3 == null))
                            .ToList();

            int totalKaryawan = data2.Count();
            ViewBag.TotalKaryawan = totalKaryawan;
            await _hubContext.Clients.All.SendAsync("UpdateDataCount", totalKaryawan);

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

                var dashboard = new Dashboard();
                {
                    dashboard.UserActiveViewModels = viewModel;
                    dashboard.UserOnlines = userLogin;
                    dashboard.Products = product;
                }

                return View(dashboard);
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

        [HttpGet]        
        public IActionResult CountNotifikasi()
        {
            var getUserId = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            var getUserActiveId = _userActiveRepository.GetAllUser().Where(u => u.UserActiveCode == getUserId.KodeUser).FirstOrDefault().UserActiveId;
            var data2 = _purchaseRequestRepository.GetAllPurchaseRequest()
                            .Where(p => (p.UserApprove1Id == getUserActiveId && p.ApproveStatusUser1 == null)
                            || (p.UserApprove2Id == getUserActiveId && p.ApproveStatusUser1 == "Approve" && p.ApproveStatusUser2 == null)
                            || (p.UserApprove3Id == getUserActiveId && p.ApproveStatusUser1 == "Approve" && p.ApproveStatusUser2 == "Approve" && p.ApproveStatusUser3 == null))
                            .ToList();

           
            var loggerData = new List<object>();

            foreach (var logger in data2)
            {
                var getUserApproveId = _approvalRepository.GetAllApproval().Where(u => u.UserApproveId == getUserActiveId && u.PurchaseRequestNumber == logger.PurchaseRequestNumber).FirstOrDefault().ApprovalId;

                var detail = new
                {
                    approvalId = getUserApproveId,
                    createdBy = _userActiveRepository.GetAllUserLogin().Where(u => u.Id == logger.CreateBy.ToString()).FirstOrDefault()?.NamaUser,
                    purchaseRequestNumber = logger.PurchaseRequestNumber,
                    createdDate = logger.CreateDateTime.ToString("dd/MM/yyyy HH:mm")
                };
                loggerData.Add(detail);
            }

            return Json(new { success = true, totalKaryawan = data2.Count, loggerDataJson = loggerData });

        }

    }
}
