using FastReport.Data;
using FastReport.Export.PdfSimple;
using FastReport.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PurchasingSystemApps.Areas.MasterData.Models;
using PurchasingSystemApps.Areas.MasterData.Repositories;
using PurchasingSystemApps.Areas.Order.Repositories;
using PurchasingSystemApps.Areas.Transaction.Models;
using PurchasingSystemApps.Areas.Transaction.Repositories;
using PurchasingSystemApps.Areas.Transaction.ViewModels;
using PurchasingSystemApps.Areas.Warehouse.Models;
using PurchasingSystemApps.Areas.Warehouse.Repositories;
using PurchasingSystemApps.Data;
using PurchasingSystemApps.Models;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace PurchasingSystemApps.Areas.Transaction.Controllers
{
    [Area("Transaction")]
    [Route("Transaction/[Controller]/[Action]")]
    public class KeyPerformanceIndicatorController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUnitRequestRepository _unitRequestRepository;
        private readonly IUserActiveRepository _userActiveRepository;
        public KeyPerformanceIndicatorController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IUnitRequestRepository UnitRequestRepository,
            IUserActiveRepository userActiveRepository
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _unitRequestRepository = UnitRequestRepository;
            _userActiveRepository = userActiveRepository;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index()
        {
            ViewBag.Active = "KeyPerformanceIndikator";
            var data = _unitRequestRepository.GetAllUnitRequest();
            return View(data);
        }

        [HttpPost]
        //public IActionResult JsonData()
        //{
        //    ViewBag.Active = "KeyPerformanceIndikator";
        //    var data = _unitRequestRepository.GetAllUnitRequest();
        //    return Json(data);
        //}
        public async Task<IActionResult> GetDataById()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = new Guid(user.Id);
            var data = _unitRequestRepository.GetAllUnitRequest().Where(r => r.CreateBy == userId).ToList();

            // Gunakan userId untuk melakukan sesuatu, misalnya:
            ViewBag.UserId = userId;
            return Json(data);
        }

        public async Task<IActionResult> Json()
        {
            var user = await _userManager.GetUserAsync(User);
            var data = _unitRequestRepository.GetAllUnitRequest();
            var userId = user.Id;
            var arrayData = new
            {
                UserId = userId,
                DataUser = data
            };

            // Gunakan userId untuk melakukan sesuatu, misalnya:
            ViewBag.UserId = userId;
            return Json(arrayData);
        }

        public async Task<IActionResult> chartJson()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = new Guid(user.Id);
            var request = _unitRequestRepository.GetAllUnitRequestAsc().Where(r => r.Status == "Request" && r.CreateBy == userId).GroupBy(u => u.CreateDateTime.ToString("MMMM yyyy")).Select(y => new
            {
                Months = y.Key,
                Counts = y.Count()
            }).ToList();
            var approved = _unitRequestRepository.GetAllUnitRequestAsc().Where(r => r.Status == "Approved" && r.CreateBy == userId).GroupBy(u => u.CreateDateTime.ToString("MMMM yyyy")).Select(y => new
            {
                Months = y.Key,
                Counts = y.Count()
            }).ToList();
            var rejected = _unitRequestRepository.GetAllUnitRequestAsc().Where(r => r.Status == "Rejected" && r.CreateBy == userId).GroupBy(u => u.CreateDateTime.ToString("MMMM yyyy")).Select(y => new
            {
                Months = y.Key,
                Counts = y.Count()
            }).ToList();
            var completed = _unitRequestRepository.GetAllUnitRequestAsc().Where(r => r.Status != "Rejected" && r.Status != "Request" && r.Status != "Approved" && r.CreateBy == userId).GroupBy(u => u.CreateDateTime.ToString("MMMM yyyy")).Select(y => new
            {
                Months = y.Key,
                Counts = y.Count()
            }).ToList();

            var arrayData = new
            {
                UserId = userId,
                CountRequest = request,
                CountApproved = approved,
                CountRejected = rejected,
                CountCompleted = completed
            };
            ViewBag.UserId = userId;
            return Json(arrayData);
        }
    }
}
