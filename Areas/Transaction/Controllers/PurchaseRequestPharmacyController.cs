using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting.Internal;
using PurchasingSystem.Areas.MasterData.Repositories;
using PurchasingSystem.Areas.Order.Models;
using PurchasingSystem.Areas.Order.Repositories;
using PurchasingSystem.Data;
using PurchasingSystem.Hubs;
using PurchasingSystem.Models;
using PurchasingSystem.Repositories;
using System.Data.SqlClient;
using System.Security.Claims;

namespace PurchasingSystemStaging.Areas.Transaction.Controllers
{
    [Area("Transaction")]
    [Route("Transaction/[Controller]/[Action]")]
    public class PurchaseRequestPharmacyController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IPurchaseRequestRepository _purchaseRequestRepository;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IProductRepository _productRepository;
        private readonly ITermOfPaymentRepository _termOfPaymentRepository;
        private readonly IApprovalPurchaseRequestRepository _approvalRepository;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IPositionRepository _positionRepository;
        private readonly ILeadTimeRepository _leadTimeRepository;
        private readonly ISupplierRepository _supplierRepository;
        private readonly IHubContext<ChatHub> _hubContext;

        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        public PurchaseRequestPharmacyController
        (
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IPurchaseRequestRepository purchaseRequestRepository,
            IUserActiveRepository userActiveRepository,
            IProductRepository productRepository,
            ITermOfPaymentRepository termOfPaymentRepository,
            IApprovalPurchaseRequestRepository approvalRepository,
            IPurchaseOrderRepository purchaseOrderRepository,
            IDepartmentRepository departmentRepository,
            IPositionRepository positionRepository,
            ILeadTimeRepository leadTimeRepository,
            ISupplierRepository supplierRepository,
            IHubContext<ChatHub> hubContext,
            
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _purchaseRequestRepository = purchaseRequestRepository;
            _userActiveRepository = userActiveRepository;
            _productRepository = productRepository;
            _termOfPaymentRepository = termOfPaymentRepository;
            _approvalRepository = approvalRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
            _departmentRepository = departmentRepository;
            _positionRepository = positionRepository;
            _leadTimeRepository = leadTimeRepository;
            _supplierRepository = supplierRepository;
            _hubContext = hubContext;
           
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
        }

        [HttpGet]
        //[Authorize(Roles = "AddPurchaseRequest")]
        public async Task<IActionResult> AddPurchaseRequest(string searchTerm)
        {
            ViewBag.Active = "AddPurchaseRequest";

            _signInManager.IsSignedIn(User);
            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.FindFirst(ClaimTypes.Name).Value).FirstOrDefault();

            ViewBag.TermOfPayment = new SelectList(await _termOfPaymentRepository.GetTermOfPayments(), "TermOfPaymentId", "TermOfPaymentName", SortOrder.Ascending);
            ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
            ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
            ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);

            PurchaseRequest purchaseRequest = new PurchaseRequest()
            {
                UserAccessId = getUser.Id,
            };
            purchaseRequest.PurchaseRequestDetails.Add(new PurchaseRequestDetail() { PurchaseRequestDetailId = Guid.NewGuid() });
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _purchaseRequestRepository.GetAllPurchaseRequest().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.PurchaseRequestNumber).FirstOrDefault();
            if (lastCode == null)
            {
                purchaseRequest.PurchaseRequestNumber = "PR" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.PurchaseRequestNumber.Substring(2, 6);

                if (lastCodeTrim != setDateNow)
                {
                    purchaseRequest.PurchaseRequestNumber = "PR" + setDateNow + "0001";
                }
                else
                {
                    purchaseRequest.PurchaseRequestNumber = "PR" + setDateNow + (Convert.ToInt32(lastCode.PurchaseRequestNumber.Substring(9, lastCode.PurchaseRequestNumber.Length - 9)) + 1).ToString("D4");
                }
            }

            return View(purchaseRequest);
        }

        [HttpPost]
        //[Authorize(Roles = "AddPurchaseRequest")]
        public async Task<IActionResult> AddPurchaseRequest(PurchaseRequest model)
        {
            ViewBag.Active = "AddPurchaseRequest";
            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _purchaseRequestRepository.GetAllPurchaseRequest().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.PurchaseRequestNumber).FirstOrDefault();
            if (lastCode == null)
            {
                model.PurchaseRequestNumber = "PR" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.PurchaseRequestNumber.Substring(2, 6);

                if (lastCodeTrim != setDateNow)
                {
                    model.PurchaseRequestNumber = "PR" + setDateNow + "0001";
                }
                else
                {
                    model.PurchaseRequestNumber = "PR" + setDateNow + (Convert.ToInt32(lastCode.PurchaseRequestNumber.Substring(9, lastCode.PurchaseRequestNumber.Length - 9)) + 1).ToString("D4");
                }
            }

            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.FindFirst(ClaimTypes.Name).Value).FirstOrDefault();

            if (ModelState.IsValid)
            {
                var purchaseRequest = new PurchaseRequest
                {
                    CreateDateTime = DateTimeOffset.Now,
                    CreateBy = new Guid(getUser.Id), //Convert Guid to String
                    PurchaseRequestId = model.PurchaseRequestId,
                    PurchaseRequestNumber = model.PurchaseRequestNumber,
                    UserAccessId = getUser.Id,
                    Department1Id = model.Department1Id,
                    Position1Id = model.Position1Id,
                    UserApprove1Id = model.UserApprove1Id,
                    ApproveStatusUser1 = model.ApproveStatusUser1,
                    Department2Id = model.Department2Id,
                    Position2Id = model.Position2Id,
                    UserApprove2Id = model.UserApprove2Id,
                    ApproveStatusUser2 = model.ApproveStatusUser2,
                    Department3Id = model.Department3Id,
                    Position3Id = model.Position3Id,
                    UserApprove3Id = model.UserApprove3Id,
                    ApproveStatusUser3 = model.ApproveStatusUser3,
                    TermOfPaymentId = model.TermOfPaymentId,
                    Status = model.Status,
                    QtyTotal = model.QtyTotal,
                    GrandTotal = Math.Truncate(model.GrandTotal),
                    Note = model.Note,
                    MessageApprove1 = model.MessageApprove1,
                    MessageApprove2 = model.MessageApprove2,
                    MessageApprove3 = model.MessageApprove3
                };

                var ItemsList = new List<PurchaseRequestDetail>();

                foreach (var item in model.PurchaseRequestDetails)
                {
                    ItemsList.Add(new PurchaseRequestDetail
                    {
                        CreateDateTime = DateTimeOffset.Now,
                        CreateBy = new Guid(getUser.Id),
                        ProductNumber = item.ProductNumber,
                        ProductName = item.ProductName,
                        Measurement = item.Measurement,
                        Supplier = item.Supplier,
                        Qty = item.Qty,
                        Price = Math.Truncate(item.Price),
                        Discount = item.Discount,
                        SubTotal = Math.Truncate(item.SubTotal)
                    });
                }

                var getItemData = ItemsList.Where(a => a.Supplier != null).FirstOrDefault();

                var getSupplier = _supplierRepository.GetAllSupplier().Where(s => s.SupplierName == getItemData.Supplier).FirstOrDefault();

                var getLeadTime = getSupplier.LeadTime.LeadTimeValue;

                var expiredDay = DateTimeOffset.Now.Date.AddDays(Convert.ToDouble(getLeadTime));

                var remainingDay = DateTimeOffset.Now.Date - purchaseRequest.CreateDateTime.Date;

                purchaseRequest.ExpiredDay = getLeadTime;
                purchaseRequest.ExpiredDate = expiredDay;
                purchaseRequest.RemainingDay = purchaseRequest.ExpiredDay - remainingDay.Days;

                purchaseRequest.PurchaseRequestDetails = ItemsList;
                _purchaseRequestRepository.Tambah(purchaseRequest);

                ////Signal R
                //var data2 = _purchaseRequestRepository.GetAllPurchaseRequest();
                //int totalKaryawan = data2.Count();
                //await _hubContext.Clients.All.SendAsync("UpdateDataCount", totalKaryawan);
                ////End Signal R                

                if (model.UserApprove1Id != null)
                {
                    var approval = new ApprovalPurchaseRequest
                    {
                        CreateDateTime = DateTimeOffset.Now,
                        CreateBy = new Guid(getUser.Id),
                        PurchaseRequestId = purchaseRequest.PurchaseRequestId,
                        PurchaseRequestNumber = purchaseRequest.PurchaseRequestNumber,
                        UserAccessId = getUser.Id.ToString(),
                        ExpiredDay = purchaseRequest.ExpiredDay,
                        RemainingDay = purchaseRequest.RemainingDay,
                        ExpiredDate = purchaseRequest.ExpiredDate,
                        UserApproveId = purchaseRequest.UserApprove1Id,
                        ApproveBy = "",
                        ApprovalTime = "",
                        ApprovalDate = DateTimeOffset.MinValue,
                        ApprovalStatusUser = "User1",
                        Status = purchaseRequest.Status,
                        Note = purchaseRequest.Note,
                        Message = purchaseRequest.MessageApprove1
                    };
                    _approvalRepository.Tambah(approval);
                }

                if (model.UserApprove2Id != null)
                {
                    var approval = new ApprovalPurchaseRequest
                    {
                        CreateDateTime = DateTimeOffset.Now,
                        CreateBy = new Guid(getUser.Id),
                        PurchaseRequestId = purchaseRequest.PurchaseRequestId,
                        PurchaseRequestNumber = purchaseRequest.PurchaseRequestNumber,
                        UserAccessId = getUser.Id.ToString(),
                        ExpiredDay = purchaseRequest.ExpiredDay,
                        RemainingDay = purchaseRequest.RemainingDay,
                        ExpiredDate = purchaseRequest.ExpiredDate,
                        UserApproveId = purchaseRequest.UserApprove2Id,
                        ApproveBy = "",
                        ApprovalTime = "",
                        ApprovalDate = DateTimeOffset.MinValue,
                        ApprovalStatusUser = "User2",
                        Status = purchaseRequest.Status,
                        Note = purchaseRequest.Note,
                        Message = purchaseRequest.MessageApprove2
                    };
                    _approvalRepository.Tambah(approval);
                }

                if (model.UserApprove3Id != null)
                {
                    var approval = new ApprovalPurchaseRequest
                    {
                        CreateDateTime = DateTimeOffset.Now,
                        CreateBy = new Guid(getUser.Id),
                        PurchaseRequestId = purchaseRequest.PurchaseRequestId,
                        PurchaseRequestNumber = purchaseRequest.PurchaseRequestNumber,
                        UserAccessId = getUser.Id.ToString(),
                        ExpiredDay = purchaseRequest.ExpiredDay,
                        RemainingDay = purchaseRequest.RemainingDay,
                        ExpiredDate = purchaseRequest.ExpiredDate,
                        UserApproveId = purchaseRequest.UserApprove3Id,
                        ApproveBy = "",
                        ApprovalTime = "",
                        ApprovalDate = DateTimeOffset.MinValue,
                        ApprovalStatusUser = "User3",
                        Status = purchaseRequest.Status,
                        Note = purchaseRequest.Note,
                        Message = purchaseRequest.MessageApprove3
                    };
                    _approvalRepository.Tambah(approval);
                }

                TempData["SuccessMessage"] = "Number " + model.PurchaseRequestNumber + " Saved";
                return Json(new { redirectToUrl = Url.Action("Index", "PurchaseRequest") });
            }
            else
            {
                ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
                ViewBag.TermOfPayment = new SelectList(await _termOfPaymentRepository.GetTermOfPayments(), "TermOfPaymentId", "TermOfPaymentName", SortOrder.Ascending);
                ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
                ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
                TempData["WarningMessage"] = "Please, input all data !";
                return View(model);
            }
        }
    }
}
