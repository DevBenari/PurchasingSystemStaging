using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PurchasingSystemApps.Areas.MasterData.Models;
using PurchasingSystemApps.Areas.MasterData.Repositories;
using PurchasingSystemApps.Areas.MasterData.ViewModels;
using PurchasingSystemApps.Areas.Order.Models;
using PurchasingSystemApps.Areas.Order.Repositories;
using PurchasingSystemApps.Areas.Order.ViewModels;
using PurchasingSystemApps.Data;
using PurchasingSystemApps.Models;
using PurchasingSystemApps.Repositories;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace PurchasingSystemApps.Areas.Order.Controllers
{
    [Area("Order")]
    [Route("Order/[Controller]/[Action]")]
    public class ApprovalController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IApprovalRepository _approvalRepository;
        private readonly IProductRepository _productRepository;
        private readonly IPurchaseRequestRepository _purchaseRequestRepository;
        private readonly ITermOfPaymentRepository _termOfPaymentRepository;

        private readonly IHostingEnvironment _hostingEnvironment;

        public ApprovalController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IApprovalRepository ApprovalRepository,
            IUserActiveRepository userActiveRepository,
            IProductRepository productRepository,
            IPurchaseRequestRepository purchaseRequestRepository,
            ITermOfPaymentRepository termOfPaymentRepository,

            IHostingEnvironment hostingEnvironment
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _approvalRepository = ApprovalRepository;
            _userActiveRepository = userActiveRepository;
            _productRepository = productRepository;
            _purchaseRequestRepository = purchaseRequestRepository;
            _termOfPaymentRepository = termOfPaymentRepository;

            _hostingEnvironment = hostingEnvironment;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            ViewBag.Active = "Order";

            var countApproval = _applicationDbContext.Approvals.Where(p => p.Status == "Waiting Approval").GroupBy(u => u.PurchaseRequestId).Select(y => new
            {
                ApprovalId = y.Key,
                CountOfApprovals = y.Count()
            }).ToList();
            ViewBag.CountApproval= countApproval.Count;

            var countPurchaseRequest = _applicationDbContext.PurchaseRequests.Where(p => p.Status == "Waiting Approval").GroupBy(u => u.PurchaseRequestId).Select(y => new
            {
                PurchaseRequestId = y.Key,
                CountOfPurchaseRequests = y.Count()
            }).ToList();
            ViewBag.CountPurchaseRequest = countPurchaseRequest.Count;

            var getUserLogin = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            var getUserActive = _userActiveRepository.GetAllUser().Where(c => c.UserActiveCode == getUserLogin.KodeUser).FirstOrDefault();

            if (getUserLogin.Id == "5f734880-f3d9-4736-8421-65a66d48020e")
            {
                var data = _approvalRepository.GetAllApproval();
                return View(data);
            }
            else 
            {
                var data = _approvalRepository.GetAllApproval().Where(u => u.UserApproveId == getUserActive.UserActiveId).ToList();
                return View(data);
            }                        
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Index(DateTime tglAwalPencarian, DateTime tglAkhirPencarian)
        {
            ViewBag.Active = "Order";
            ViewBag.tglAwalPencarian = tglAwalPencarian.ToString("dd MMMM yyyy");
            ViewBag.tglAkhirPencarian = tglAkhirPencarian.ToString("dd MMMM yyyy");

            var data = _approvalRepository.GetAllApproval().Where(r => r.CreateDateTime.Date >= tglAwalPencarian && r.CreateDateTime.Date <= tglAkhirPencarian).ToList();
            return View(data);
        }
       
        [HttpGet]
        [AllowAnonymous]
        public async Task<ViewResult> DetailApproval(Guid Id)
        {
            ViewBag.Active = "Order";

            ViewBag.User = new SelectList(_userManager.Users, nameof(ApplicationUser.Id), nameof(ApplicationUser.NamaUser), SortOrder.Ascending);
            ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
            ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
            ViewBag.TermOfPayment = new SelectList(await _termOfPaymentRepository.GetTermOfPayments(), "TermOfPaymentId", "TermOfPaymentName", SortOrder.Ascending);

            var Approval = await _approvalRepository.GetApprovalById(Id);
            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            if (Approval == null)
            {
                Response.StatusCode = 404;
                return View("ApprovalNotFound", Id);
            }

            ApprovalViewModel viewModel = new ApprovalViewModel()
            {
                ApprovalId = Approval.ApprovalId,
                PurchaseRequestId = Approval.PurchaseRequestId,
                PurchaseRequestNumber = Approval.PurchaseRequestNumber,
                UserAccessId = Approval.UserAccessId,
                DueDateId = Approval.DueDateId,
                UserApproveId = Approval.UserApproveId,
                UserApprove = getUser.Email,
                ApproveTime = "",
                ApproveBy = getUser.NamaUser,
                ApproveDate = DateTime.Now,
                ApproveStatusUser = Approval.ApproveStatusUser,
                Status = Approval.Status,
                Note = Approval.Note
            };

            var getPrNumber = _purchaseRequestRepository.GetAllPurchaseRequest().Where(pr => pr.PurchaseRequestNumber == viewModel.PurchaseRequestNumber).FirstOrDefault();            

            viewModel.QtyTotal = getPrNumber.QtyTotal;
            viewModel.GrandTotal = Math.Truncate(getPrNumber.GrandTotal);

            var ItemsList = new List<PurchaseRequestDetail>();

            foreach (var item in getPrNumber.PurchaseRequestDetails)
            {
                ItemsList.Add(new PurchaseRequestDetail
                {
                    ProductNumber = item.ProductNumber,
                    ProductName = item.ProductName,
                    Principal = item.Principal,
                    Measurement = item.Measurement,
                    Qty = item.Qty,
                    Price = Math.Truncate(item.Price),
                    Discount = item.Discount,
                    SubTotal = Math.Truncate(item.SubTotal)
                });
            }

            viewModel.PurchaseRequestDetails = ItemsList;

            return View(viewModel);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> DetailApproval(ApprovalViewModel viewModel)
        {
            ViewBag.Active = "Order";

            if (ModelState.IsValid)
            {
                var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();                

                var approval = await _approvalRepository.GetApprovalByIdNoTracking(viewModel.ApprovalId);
                var checkPR = _purchaseRequestRepository.GetAllPurchaseRequest().Where(c => c.PurchaseRequestNumber == viewModel.PurchaseRequestNumber).FirstOrDefault();

                //approval.Status = viewModel.Status;
                //approval.ApproveDate = DateTime.Now;
                //approval.ApproveBy = getUser.NamaUser;
                //approval.Note = viewModel.Note;

                if (checkPR != null)
                {
                    if (approval.ApproveStatusUser == "User1")
                    {
                        var updateStatusUser1 = _approvalRepository.GetAllApproval().Where(c => c.ApproveStatusUser == "User1").FirstOrDefault();
                        if (updateStatusUser1 != null)
                        {
                            updateStatusUser1.Status = viewModel.Status;
                            updateStatusUser1.ApproveDate = DateTime.Now;
                            updateStatusUser1.ApproveBy = getUser.NamaUser;
                            updateStatusUser1.Note = viewModel.Note;

                            _applicationDbContext.Entry(updateStatusUser1).State = EntityState.Modified;
                        }

                        var updateStatusUser2 = _approvalRepository.GetAllApproval().Where(c => c.ApproveStatusUser == "User2").FirstOrDefault();
                        if (updateStatusUser2 != null) 
                        {
                            updateStatusUser2.Status = "User1Approve";

                            _applicationDbContext.Entry(updateStatusUser2).State = EntityState.Modified;
                        }
                        
                        checkPR.ApproveStatusUser1 = viewModel.Status;
                    }
                    else if (approval.ApproveStatusUser == "User2")
                    {
                        var updateStatusUser2 = _approvalRepository.GetAllApproval().Where(c => c.ApproveStatusUser == "User2").FirstOrDefault();
                        if (updateStatusUser2 != null)
                        {
                            updateStatusUser2.Status = viewModel.Status;
                            updateStatusUser2.ApproveDate = DateTime.Now;
                            updateStatusUser2.ApproveBy = getUser.NamaUser;
                            updateStatusUser2.Note = viewModel.Note;

                            _applicationDbContext.Entry(updateStatusUser2).State = EntityState.Modified;
                        }

                        var updateStatusUser3 = _approvalRepository.GetAllApproval().Where(c => c.ApproveStatusUser == "User3").FirstOrDefault();
                        if (updateStatusUser3 != null)
                        {
                            updateStatusUser3.Status = "User2Approve";

                            _applicationDbContext.Entry(updateStatusUser3).State = EntityState.Modified;
                        }                        
                        checkPR.ApproveStatusUser2 = viewModel.Status;
                    }
                    else if (approval.ApproveStatusUser == "User3")
                    {
                        var updateStatusUser3 = _approvalRepository.GetAllApproval().Where(c => c.ApproveStatusUser == "User3").FirstOrDefault();
                        if (updateStatusUser3 != null)
                        {
                            updateStatusUser3.Status = viewModel.Status;
                            updateStatusUser3.ApproveDate = DateTime.Now;
                            updateStatusUser3.ApproveBy = getUser.NamaUser;
                            updateStatusUser3.Note = viewModel.Note;

                            _applicationDbContext.Entry(updateStatusUser3).State = EntityState.Modified;
                        }
                        checkPR.ApproveStatusUser3 = viewModel.Status;
                    }

                    if (checkPR.ApproveStatusUser1 == "Approve" && checkPR.ApproveStatusUser2 == "Approve" && checkPR.ApproveStatusUser3 == "Approve")
                    {
                        checkPR.Status = viewModel.Status;
                        checkPR.Note = viewModel.Note;
                    }                    

                    _approvalRepository.Update(approval);
                    _applicationDbContext.Entry(checkPR).State = EntityState.Modified;                    
                    _applicationDbContext.SaveChanges();
                }                

                TempData["SuccessMessage"] = "Saved";
                return RedirectToAction("Index", "Approval");
            }

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> RejectApproval(ApprovalViewModel viewModel)
        {
            ViewBag.Active = "Order";

            viewModel.Status = "Reject";

            if (ModelState.IsValid)
            {
                Approval approval = await _approvalRepository.GetApprovalByIdNoTracking(viewModel.ApprovalId);

                approval.Status = viewModel.Status;
                approval.Note = viewModel.Note;

                if (approval.Status == "Approve" || approval.Status == "Reject")
                {
                    //Belum di sesuaikan

                    //approval.ApproveDate = DateTime.Now;
                    //approval.ApproveBy = viewModel.ApproveBy;
                }

                var checkApprove = _approvalRepository.GetAllApproval().Where(a => a.PurchaseRequestNumber == viewModel.PurchaseRequestNumber && a.UserApproveId.ToString() == viewModel.UserAccessId).ToList();

                var result = _purchaseRequestRepository.GetAllPurchaseRequest().Where(c => c.PurchaseRequestNumber == viewModel.PurchaseRequestNumber).FirstOrDefault();
                if (result != null)
                {
                    result.Status = viewModel.Status;
                    result.Note = viewModel.Note;

                    _applicationDbContext.Entry(result).State = EntityState.Modified;
                }
                _approvalRepository.Update(approval);

                if (approval.Status == "Waiting Approval")
                {
                    TempData["SuccessMessage"] = "Number " + viewModel.PurchaseRequestNumber + " Waiting Approval";
                }
                else if (approval.Status == "Approved")
                {
                    TempData["SuccessMessage"] = "Number " + viewModel.PurchaseRequestNumber + " Approved";
                }
                else if (approval.Status == "Rejected")
                {
                    TempData["SuccessMessage"] = "Number " + viewModel.PurchaseRequestNumber + " Rejected";
                }
                return RedirectToAction("Index", "PurchaseOrder");
            }

            return View();
        }
    }
}
