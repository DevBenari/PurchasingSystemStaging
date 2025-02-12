using FastReport;
using FastReport.Data;
using FastReport.Export.PdfSimple;
using FastReport.Format;
using FastReport.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol;
using PurchasingSystem.Areas.MasterData.Models;
using PurchasingSystem.Areas.MasterData.Repositories;
using PurchasingSystem.Areas.MasterData.ViewModels;
using PurchasingSystem.Areas.Order.Models;
using PurchasingSystem.Areas.Order.Repositories;
using PurchasingSystem.Areas.Order.ViewModels;
using PurchasingSystem.Areas.Transaction.Repositories;
using PurchasingSystem.Areas.Transaction.Models;
using PurchasingSystem.Areas.Warehouse.Repositories;
using PurchasingSystem.Areas.Warehouse.Models;
using PurchasingSystem.Data;
using PurchasingSystem.Models;
using PurchasingSystem.Repositories;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Metrics;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
using System;
using System.Drawing;
namespace PurchasingSystemStaging.Areas.General.Controllers
{
    [Area("General")]
    [Route("General/[Controller]/[Action]")]
    public class KeyPerformanceIndicatorController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IApprovalPurchaseRequestRepository _approvalRepository;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IPurchaseRequestRepository _purchaseRequestRepository;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IPositionRepository _positionRepository;
        private readonly IUnitRequestRepository _unitRequestRepository;
        private readonly IQtyDifferenceRepository _qtyDifferenceRepository;
        private readonly IProductReturnRepository _productReturnRepository;

        private readonly IDataProtector _protector;
        private readonly UrlMappingService _urlMappingService;

        public KeyPerformanceIndicatorController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IApprovalPurchaseRequestRepository approvalRepository,
            IPurchaseOrderRepository purchaseOrderRepository,
            IPurchaseRequestRepository purchaseRequestRepository,
            IUserActiveRepository userActiveRepository,
            IPositionRepository positionRepository,
            IUnitRequestRepository unitRequestRepository,
            IQtyDifferenceRepository qtyDifferenceRepository,
            IProductReturnRepository productReturnRepository,

            IDataProtectionProvider provider,
            UrlMappingService urlMappingService
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _approvalRepository = approvalRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
            _purchaseRequestRepository = purchaseRequestRepository;
            _userActiveRepository = userActiveRepository;
            _positionRepository = positionRepository;
            _unitRequestRepository = unitRequestRepository;
            _qtyDifferenceRepository = qtyDifferenceRepository;
            _productReturnRepository = productReturnRepository;

            _protector = provider.CreateProtector("UrlProtector");
            _urlMappingService = urlMappingService;
        }

        public IActionResult RedirectToIndex()
        {
            try
            {
                ViewBag.Active = "KeyPerformanceIndikator";
                string originalPath = $"Page:General/KeyPerformanceIndicator/Index";
                string encryptedPath = _protector.Protect(originalPath);

                // Hash GUID-like code (SHA256 truncated to 36 characters)
                string guidLikeCode = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(encryptedPath)))
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .Substring(0, 36);

                // Simpan mapping GUID-like code ke encryptedPath di penyimpanan sementara (misalnya, cache)
                _urlMappingService.InMemoryMapping[guidLikeCode] = encryptedPath;

                return Redirect("/" + guidLikeCode);
            }
            catch
            {
                // Jika enkripsi gagal, kembalikan view
                return Redirect(Request.Path);
            }
        }

        [HttpGet]

        public IActionResult Index()
        {
            ViewBag.Active = "KeyPerformanceIndikator";

            var data = _purchaseRequestRepository.GetAllPurchaseRequest();

            var checkUserLogin = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            var getUserActive = _userActiveRepository.GetAllUser().Where(c => c.UserActiveCode == checkUserLogin.KodeUser).FirstOrDefault();
            var userLogin = _userActiveRepository.GetAllUserLogin().Where(u => u.IsOnline == true).ToList();
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
                    UserPhotoPath = user.Foto,
                    IsActive = user.IsActive
                };
                return View(viewModel);
            }
            else if (user == null && checkUserLogin.NamaUser == "SuperAdmin")
            {
                UserActiveViewModel viewModel = new UserActiveViewModel
                {
                    UserActiveCode = checkUserLogin.KodeUser,
                    FullName = checkUserLogin.NamaUser,
                    Email = checkUserLogin.Email,
                };
                return View(viewModel);
            }

            return View("Error");
        }

        [HttpPost]
        public async Task<IActionResult> JsonPosition(Selected model)
        {
            ViewBag.Active = "KeyPerformanceIndikator";
            var getUserLogin = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            var getUserActive = _userActiveRepository.GetAllUser().Where(c => c.UserActiveCode == getUserLogin.KodeUser).FirstOrDefault();
            var user = await _userManager.GetUserAsync(User);
            var getParam = model.Position;
            var getPosition = _userActiveRepository.GetAllUser().Where(x => x.Position.PositionName.Contains(getParam)).ToList();
            //var getCreatedById = _purchaseRequestRepository.GetAllPurchaseRequest().Where(x => x.CreateBy == getUserLogin.Id);
            return Json(getPosition);
        }

        public async Task<IActionResult> JsonCount(Selected model)
        {
            ViewBag.Active = "KeyPerformanceIndikator";
            var getUserLogin = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            var getUserActive = _userActiveRepository.GetAllUser().Where(c => c.UserActiveCode == getUserLogin.KodeUser).FirstOrDefault();
            var user = await _userManager.GetUserAsync(User);
            var getMonth = model.Month;
            var getYear = model.Year;
            var getUser = model.User;
            var getPosition = model.Position;

            // Month
            if (getMonth == "")
            {
                DateTime Month = DateTime.Now;
                getMonth = Month.ToString("MM");
            } else {
                getMonth = model.Month;
            }

            // Year
            if (getYear == "")
            {
                DateTime Year = DateTime.Now;
                getYear = Year.ToString("yyyy");
            } else {
                getYear = model.Year;
            }
            
            var selectDate = getMonth + "-" + getYear;
            //var selectData = new
            //{
            //    Email = getUser,
            //    Month = getMonth,
            //    Year = getYear,
            //    Position = getPosition,
            //};

            var dataNetUsers = _userActiveRepository.GetAllUserLogin().Where(x => x.Email == getUser).ToList();
            var dataUsersActive = _userActiveRepository.GetAllUser().Where(x => x.Email == getUser).ToList();
            var idNetUsers = new Guid(dataNetUsers[0].Id);
            var idUsersActive = dataUsersActive[0].UserActiveId;

            //Created By
            var dataPR = _purchaseRequestRepository.GetAllPurchaseRequest().Where(d => d.CreateBy == idNetUsers && d.CreateDateTime.ToString("MM-yyyy") == selectDate).Count();
            var dataUnitRequest = _unitRequestRepository.GetAllUnitRequest().Where(d => d.CreateBy == idNetUsers && d.CreateDateTime.ToString("MM-yyyy") == selectDate).Count();
            var dataQtyDiff = _qtyDifferenceRepository.GetAllQtyDifference().Where(d => d.CreateBy == idNetUsers && d.CreateDateTime.ToString("MM-yyyy") == selectDate).Count();
            var dataProductReturn = _productReturnRepository.GetAllProductReturn().Where(d => d.CreateBy == idNetUsers && d.CreateDateTime.ToString("MM-yyyy") == selectDate).Count();

            //Waiting By
            var waitDataPR = _purchaseRequestRepository.GetAllPurchaseRequest().Where(w => w.UserApprove1Id == idUsersActive && w.Status == "Waiting Approval" && w.CreateDateTime.ToString("MM-yyyy") == selectDate || w.UserApprove2Id == idUsersActive && w.Status == "Waiting Approval" && w.CreateDateTime.ToString("MM-yyyy") == selectDate || w.UserApprove3Id == idUsersActive && w.Status == "Waiting Approval" && w.CreateDateTime.ToString("MM-yyyy") == selectDate).Count();
            var waitDataUnitRequest = _unitRequestRepository.GetAllUnitRequest().Where(w => w.UserApprove1Id == idUsersActive && w.Status == "Waiting Approval" && w.CreateDateTime.ToString("MM-yyyy") == selectDate).Count();
            var waitDataQtyDiff = _qtyDifferenceRepository.GetAllQtyDifference().Where(w => w.UserApprove1Id == idUsersActive && w.Status == "Waiting Approval" && w.CreateDateTime.ToString("MM-yyyy") == selectDate || w.UserApprove2Id == idUsersActive && w.Status == "Waiting Approval" && w.CreateDateTime.ToString("MM-yyyy") == selectDate).Count();
            var waitDataProductReturn = _productReturnRepository.GetAllProductReturn().Where(w => w.UserApprove1Id == idUsersActive && w.ApproveStatusUser1 == "Waiting Approval" && w.CreateDateTime.ToString("MM-yyyy") == selectDate || w.UserApprove2Id == idUsersActive && w.ApproveStatusUser2 == "Waiting Approval" && w.CreateDateTime.ToString("MM-yyyy") == selectDate || w.UserApprove3Id == idUsersActive && w.ApproveStatusUser3 == "Waiting Approval" && w.CreateDateTime.ToString("MM-yyyy") == selectDate).Count();

            //Approved By 
            var approveDataPR = _purchaseOrderRepository.GetAllPurchaseOrder().Where(w => w.UserApprove1Id == idUsersActive && w.ApproveStatusUser1 == "Approve" && w.CreateDateTime.ToString("MM-yyyy") == selectDate || w.UserApprove2Id == idUsersActive && w.ApproveStatusUser2 == "Approve" && w.CreateDateTime.ToString("MM-yyyy") == selectDate || w.UserApprove3Id == idUsersActive && w.ApproveStatusUser3 == "Approve" && w.CreateDateTime.ToString("MM-yyyy") == selectDate).Count();
            var approveDataUnitRequest = _unitRequestRepository.GetAllUnitRequest().Where(w => w.UserApprove1Id == idUsersActive && w.ApproveStatusUser1 == "Approve" && w.CreateDateTime.ToString("MM-yyyy") == selectDate).Count();
            var approveDataQtyDiff = _qtyDifferenceRepository.GetAllQtyDifference().Where(w => w.UserApprove1Id == idUsersActive && w.ApproveStatusUser1 == "Approve" && w.CreateDateTime.ToString("MM-yyyy") == selectDate || w.UserApprove2Id == idUsersActive && w.ApproveStatusUser2 == "Approve" && w.CreateDateTime.ToString("MM-yyyy") == selectDate).Count();
            var approveDataProductReturn = _productReturnRepository.GetAllProductReturn().Where(w => w.UserApprove1Id == idUsersActive && w.ApproveStatusUser1 == "Approve" && w.CreateDateTime.ToString("MM-yyyy") == selectDate || w.UserApprove2Id == idUsersActive && w.ApproveStatusUser2 == "Approve" && w.CreateDateTime.ToString("MM-yyyy") == selectDate || w.UserApprove3Id == idUsersActive && w.ApproveStatusUser3 == "Approve" && w.CreateDateTime.ToString("MM-yyyy") == selectDate).Count();

            //Rejected By
            var rejectDataPR = _purchaseOrderRepository.GetAllPurchaseOrder().Where(w => w.UserApprove1Id == idUsersActive && !w.Status.Contains("RO") && w.CreateDateTime.ToString("MM-yyyy") == selectDate || w.UserApprove2Id == idUsersActive && !w.Status.Contains("RO") && w.CreateDateTime.ToString("MM-yyyy") == selectDate || w.UserApprove3Id == idUsersActive && !w.Status.Contains("RO") && w.CreateDateTime.ToString("MM-yyyy") == selectDate).Count();
            var rejectDataUnitRequest = _unitRequestRepository.GetAllUnitRequest().Where(w => w.UserApprove1Id == idUsersActive && !w.Status.Contains("RO") && w.CreateDateTime.ToString("MM-yyyy") == selectDate).Count();
            var rejectDataQtyDiff = _qtyDifferenceRepository.GetAllQtyDifference().Where(w => w.UserApprove1Id == idUsersActive && !w.Status.Contains("RO") && w.CreateDateTime.ToString("MM-yyyy") == selectDate || w.UserApprove2Id == idUsersActive && !w.Status.Contains("RO") && w.CreateDateTime.ToString("MM-yyyy") == selectDate).Count();
            var rejectDataProductReturn = _productReturnRepository.GetAllProductReturn().Where(w => w.UserApprove1Id == idUsersActive && w.ApproveStatusUser1 == "Reject" && w.CreateDateTime.ToString("MM-yyyy") == selectDate || w.UserApprove2Id == idUsersActive && w.ApproveStatusUser2 == "Reject" && w.CreateDateTime.ToString("MM-yyyy") == selectDate || w.UserApprove3Id == idUsersActive && w.ApproveStatusUser3 == "Reject" && w.CreateDateTime.ToString("MM-yyyy") == selectDate).Count();

            var dataCreated = new
            {
                TotalCreatedPR = dataPR,
                TotalCreatedUR = dataUnitRequest,
                TotalCreatedQD = dataQtyDiff,
                TotalCreatedPN = dataProductReturn,
            };

            var dataWaiting = new
            {
                TotalWaitingPR = waitDataPR,
                TotalWaitingUR = waitDataQtyDiff,
                TotalWaitingPN = waitDataProductReturn,
                TotalWaitingQD = waitDataQtyDiff
            };

            var dataApproved = new
            {
                TotalApprovedPR = approveDataPR,
                TotalApprovedUR = approveDataUnitRequest,
                TotalApprovedQD = approveDataQtyDiff,
                TotalApprovedPN = approveDataProductReturn
            };

            var dataRejected = new
            {
                TotalRejectedPR = rejectDataPR,
                TotalRejectedUR = rejectDataUnitRequest,
                TotalRejectedQD = rejectDataQtyDiff,
                TotalRejectedPN = rejectDataProductReturn
            };

            var dataMerge = new
            {
                DataCreated = dataCreated,
                DataWaiting = dataWaiting,
                DataApproved = dataApproved,
                DataRejected = dataRejected,
            };

            return Json(dataMerge);
        }

            public async Task<IActionResult> PostData(Selected model)
        {
            ViewBag.Active = "KeyPerformanceIndikator";
            var getUserLogin = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            var getUserActive = _userActiveRepository.GetAllUser().Where(c => c.UserActiveCode == getUserLogin.KodeUser).FirstOrDefault();
            var user = await _userManager.GetUserAsync(User);
            var userId = new Guid(user.Id);
            var change = model.FirstName;
            var current = "";
            var formatDate = "";

            if (change == null || change == "month")
            {
                DateTime dateTime = DateTime.Now;
                current = dateTime.ToString("MMMM yyyy");
                formatDate = "MMMM yyyy";
            }
            else
            {
                DateTime dateTime = DateTime.Now;
                current = dateTime.ToString("yyyy");
                formatDate = "yyyy";
            }

            var getAllApproval = _approvalRepository.GetAllApprovalById(getUserActive.UserActiveId).ToList();
            var getApproval = _approvalRepository.GetAllApproval().Where(a => a.UserApproveId == getUserActive.UserActiveId && a.CreateDateTime.ToString(formatDate) == current);
            var getPurchaseRequest = _purchaseRequestRepository.GetAllPurchaseRequest().Where(x => x.CreateDateTime.ToString(formatDate) == current);
            var getPurchaseOrder = _purchaseOrderRepository.GetAllPurchaseOrder().Where(a => a.UserApprove1Id == getUserActive.UserActiveId || a.UserApprove2Id == getUserActive.UserActiveId || a.UserApprove3Id == getUserActive.UserActiveId && a.CreateDateTime.ToString(formatDate) == current);
            var thisMonthPO = getPurchaseOrder.Count(x => x.Status == "In Order");
            var thisMonthPR = getPurchaseRequest.Count();
            var prCreated = getPurchaseRequest.Count(x => x.CreateBy == userId);
            var countApproval = getApproval.Count();
            var waiting_approval = getApproval.Count(x => x.Status == "Waiting Approval");
            var approved = getApproval.Count(x => x.Status == "Approve");
            var reject = getApproval.Count(x => x.Status == "Reject");
            var completed = getPurchaseOrder.Count(x => x.Status != "In Order");

            var beforeRemaining = _approvalRepository.GetChartBeforeExpired(getUserActive.UserActiveId).GroupBy(u => u.CreateDateTime.ToString(formatDate)).Select(y => new
            {
                Months = y.Key,
                BeforeExpired = y.Count()
            }).ToList();

            var moreThanExpired = _approvalRepository.GetChartMoreThanExpired(getUserActive.UserActiveId).GroupBy(u => u.CreateDateTime.ToString(formatDate)).Select(y => new
            {
                Months = y.Key,
                AfterExpired = y.Count()
            }).ToList();

            var kpiData = new
            {
                DataApproval = getApproval,
                DataPurchaseRequest = thisMonthPR,
                DataCreated = prCreated,
                DataCountApproval = countApproval,
                DataWaiting = waiting_approval,
                DataRejected = reject,
                DataApproved = approved,
                DataPurchaseOrder = thisMonthPO,
                DataCompleted = completed,
                AllData = getAllApproval,
                BeforeExpired = beforeRemaining,
                MoreThanExpired = moreThanExpired,
            };

            return Json(kpiData);
        }

        public async Task<IActionResult> KpiJson(Selected model)
        {
            ViewBag.Active = "KeyPerformanceIndikator";
            var getUserLogin = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            if (getUserLogin.Email == "superadmin@admin.com")
            {
                return View();
            }
            else
            {
                var getUserActive = _userActiveRepository.GetAllUser().Where(c => c.UserActiveCode == getUserLogin.KodeUser).FirstOrDefault();
                var user = await _userManager.GetUserAsync(User);
                var userId = new Guid(user.Id);
                var change = model.FirstName;
                var current = "";
                var formatDate = "";

                if (change == null || change == "month")
                {
                    DateTime dateTime = DateTime.Now;
                    current = dateTime.ToString("MMMM yyyy");
                    formatDate = "MMMM yyyy";
                }
                else
                {
                    DateTime dateTime = DateTime.Now;
                    current = dateTime.ToString("yyyy");
                    formatDate = "yyyy";
                }

                var getApproval = _approvalRepository.GetAllApproval().Where(a => a.UserApproveId == getUserActive.UserActiveId && a.CreateDateTime.ToString(formatDate) == current);
                var getPurchaseOrder = _purchaseOrderRepository.GetAllPurchaseOrder().Where(a => a.UserApprove1Id == getUserActive.UserActiveId || a.UserApprove2Id == getUserActive.UserActiveId || a.UserApprove3Id == getUserActive.UserActiveId && a.CreateDateTime.ToString(formatDate) == current);
                var fiveStar = getApproval.Count(x => x.RemainingDay > 0);
                var fourStar = getApproval.Count(x => x.RemainingDay == 0);
                var threeStar = getPurchaseOrder.Count(x => x.Status != "In Order");
                var twoStar = getApproval.Count(x => x.RemainingDay == -14);
                var oneStar = getApproval.Count(x => x.RemainingDay < -30);
                var data = getApproval.Count();

                var kpiRate = new
                {
                    Data = data,
                    FiveStart = fiveStar,
                    FourStar = fourStar,
                    ThreeStar = threeStar,
                    TwoStar = twoStar,
                    OneStar = oneStar
                };
                return Json(kpiRate);
            }
            //if (getUserLogin.Email == "superadmin@admin.com")
            //{
            //    return View("Error");
            //}
            //else
            //{

            //}
        }

        public async Task<IActionResult> ChartJson()
        {
            //ViewBag.Active("chartJson");
            // GET USER ACTIVE ID FROM MSTUSERACTIVE
            var getUserLogin = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            var getUserActive = _userActiveRepository.GetAllUser().Where(c => c.UserActiveCode == getUserLogin.KodeUser).FirstOrDefault();

            // GET CURRENT MONTH AND YEAR 
            DateTime dateTime = DateTime.Now;
            var current = dateTime.ToString("MMMM yyyy");

            // GET APPROVAL BY ID FROM ORDERAPPROVAL
            var getApproval = _approvalRepository.GetAllApprovalById(getUserActive.UserActiveId).ToList();
            //var groupByMonths = _approvalRepository.GetAllApprovalById(getUserActive.UserActiveId).GroupBy(u => u.CreateDateTime.ToString("MMMM yyyy")).Select(y => new
            //{
            //    Months = y.Key,
            //    Counts = y.Count()
            //}).ToList();

            var beforeRemaining = _approvalRepository.GetChartBeforeExpired(getUserActive.UserActiveId).GroupBy(u => u.CreateDateTime.ToString("MMMM yyyy")).Select(y => new
            {
                Months = y.Key,
                BeforeExpired = y.Count()
            }).ToList();


            //var onExpired = _approvalRepository.GetChartOnExpired(getUserActive.UserActiveId).GroupBy(u => u.CreateDateTime.ToString("MMMM yyyy")).Select(y => new
            //{
            //    Months = y.Key,
            //    Counts = y.Count()
            //}).ToList();

            var moreThanExpired = _approvalRepository.GetChartMoreThanExpired(getUserActive.UserActiveId).GroupBy(u => u.CreateDateTime.ToString("MMMM yyyy")).Select(y => new
            {
                Months = y.Key,
                AfterExpired = y.Count()
            }).ToList();

            var dataView = new
            {
                AllData = getApproval,
                BeforeExpired = beforeRemaining,
                //OnExpired = onExpired,
                MoreThanExpired = moreThanExpired,
            };

            return Json(dataView);
        }

    }
}
