﻿﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PurchasingSystem.Areas.MasterData.Models;
using PurchasingSystem.Areas.MasterData.Repositories;
using PurchasingSystem.Areas.MasterData.ViewModels;
using PurchasingSystem.Areas.Order.Models;
using PurchasingSystem.Areas.Order.Repositories;
using PurchasingSystem.Areas.Order.ViewModels;
using PurchasingSystem.Data;
using PurchasingSystem.Hubs;
using PurchasingSystem.Models;
using PurchasingSystem.Repositories;
using System.Security.Cryptography;
using System.Text;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace PurchasingSystem.Areas.Order.Controllers
{
    [Area("Order")]
    [Route("Order/[Controller]/[Action]")]
    public class ApprovalPurchaseRequestController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IApprovalPurchaseRequestRepository _approvalRepository;
        private readonly IProductRepository _productRepository;
        private readonly IPurchaseRequestRepository _purchaseRequestRepository;
        private readonly ITermOfPaymentRepository _termOfPaymentRepository;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IHubContext<ChatHub> _hubContext;

        private readonly IDataProtector _protector;
        private readonly UrlMappingService _urlMappingService;
        private readonly IHostingEnvironment _hostingEnvironment;

        public ApprovalPurchaseRequestController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IApprovalPurchaseRequestRepository ApprovalRepository,
            IUserActiveRepository userActiveRepository,
            IProductRepository productRepository,
            IPurchaseRequestRepository purchaseRequestRepository,
            ITermOfPaymentRepository termOfPaymentRepository,
            IPurchaseOrderRepository purchaseOrderRepository,
            IHubContext<ChatHub> hubContext,

            IDataProtectionProvider provider,
            UrlMappingService urlMappingService,
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
            _purchaseOrderRepository = purchaseOrderRepository;
            _hubContext = hubContext;

            _protector = provider.CreateProtector("UrlProtector");
            _urlMappingService = urlMappingService;
            _hostingEnvironment = hostingEnvironment;
        }        

        [HttpGet]
        [Authorize(Roles = "ReadApprovalPurchaseRequest")]
        public async Task<IActionResult> Index(string filterOptions = "", string searchTerm = "", DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, int page = 1, int pageSize = 10)
        {
            ViewBag.Active = "ApprovalPurchaseRequest";
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

            var getUserLogin = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            
            if (getUserLogin.Email == "superadmin@admin.com")
            {
                var data = await _approvalRepository.GetAllApprovalPageSize(searchTerm, page, pageSize, startDate, endDate);

                foreach (var item in data.approvals)
                {
                    if (item.Status == "Approve")
                    {
                        var updateData = _approvalRepository.GetAllApproval().Where(u => u.ApprovalId == item.ApprovalId).FirstOrDefault();

                        updateData.RemainingDay = 0;

                        _applicationDbContext.Approvals.Update(updateData);
                        _applicationDbContext.SaveChanges();
                    }
                    else
                    {
                        var remainingDay = DateTimeOffset.Now.Date - item.CreateDateTime.Date;
                        var updateData = _approvalRepository.GetAllApproval().Where(u => u.ApprovalId == item.ApprovalId).FirstOrDefault();

                        if (updateData.RemainingDay != 0)
                        {
                            updateData.RemainingDay = item.ExpiredDay - remainingDay.Days;

                            _applicationDbContext.Approvals.Update(updateData);
                            _applicationDbContext.SaveChanges();
                        }
                    }                        
                }

                var model = new Pagination<ApprovalPurchaseRequest>
                {
                    Items = data.approvals,
                    TotalCount = data.totalCountApprovals,
                    PageSize = pageSize,
                    CurrentPage = page,
                };

                // Sertakan semua parameter untuk pagination
                ViewBag.FilterOptions = filterOptions;
                ViewBag.StartDateParam = startDate?.ToString("yyyy-MM-dd");
                ViewBag.EndDateParam = endDate?.ToString("yyyy-MM-dd");
                ViewBag.PageSize = pageSize;

                return View(model);
            }
            else
            {
                var getUserActive = _userActiveRepository.GetAllUser().Where(c => c.UserActiveCode == getUserLogin.KodeUser).FirstOrDefault();
                var getUser1 = _approvalRepository.GetAllApproval().Where(a => a.ApprovalStatusUser == "User1" && a.UserApproveId == getUserActive.UserActiveId && a.Status == "Waiting Approval").ToList();
                var getUser2 = _approvalRepository.GetAllApproval().Where(a => a.ApprovalStatusUser == "User2" && a.UserApproveId == getUserActive.UserActiveId && a.Status == "User1Approve").ToList();
                var getUser3 = _approvalRepository.GetAllApproval().Where(a => a.ApprovalStatusUser == "User3" && a.UserApproveId == getUserActive.UserActiveId && a.Status == "User2Approve").ToList();

                var getUser1Approve = _approvalRepository.GetAllApproval().Where(a => a.ApprovalStatusUser == "User1" && a.UserApproveId == getUserActive.UserActiveId && a.Status == "Approve").ToList();
                var getUser2Approve = _approvalRepository.GetAllApproval().Where(a => a.ApprovalStatusUser == "User2" && a.UserApproveId == getUserActive.UserActiveId && a.Status == "Approve").ToList();
                var getUser3Approve = _approvalRepository.GetAllApproval().Where(a => a.ApprovalStatusUser == "User3" && a.UserApproveId == getUserActive.UserActiveId && a.Status == "Approve").ToList();

                var itemList = new List<ApprovalPurchaseRequest>();

                if (getUser1 != null && getUser2 != null && getUser3 != null)
                {
                    itemList.AddRange(getUser1);
                    itemList.AddRange(getUser2);
                    itemList.AddRange(getUser3);

                    itemList.AddRange(getUser1Approve);
                    itemList.AddRange(getUser2Approve);
                    itemList.AddRange(getUser3Approve);

                    //Cek apakah sudah di Approve? Jika belum jalankan Remaining Day, jika sudah lewatkan Remaining Day

                    foreach (var item in itemList)
                    {
                        if (item.Status == "Approve")
                        {
                            var updateData = _approvalRepository.GetAllApproval().Where(u => u.ApprovalId == item.ApprovalId).FirstOrDefault();

                            updateData.RemainingDay = 0;

                            _applicationDbContext.Approvals.Update(updateData);
                            _applicationDbContext.SaveChanges();
                        }
                        else
                        {
                            var remainingDay = DateTimeOffset.Now.Date - item.CreateDateTime.Date;
                            var updateData = _approvalRepository.GetAllApproval().Where(u => u.ApprovalId == item.ApprovalId).FirstOrDefault();

                            if (updateData.RemainingDay != 0)
                            {
                                updateData.RemainingDay = item.ExpiredDay - remainingDay.Days;

                                _applicationDbContext.Approvals.Update(updateData);
                                _applicationDbContext.SaveChanges();
                            }
                        }                        
                    }

                    var model = new Pagination<ApprovalPurchaseRequest>
                    {
                        Items = itemList,
                        TotalCount = itemList.Count,
                        PageSize = pageSize,
                        CurrentPage = page,
                    };

                    // Sertakan semua parameter untuk pagination
                    ViewBag.FilterOptions = filterOptions;
                    ViewBag.StartDateParam = startDate?.ToString("yyyy-MM-dd");
                    ViewBag.EndDateParam = endDate?.ToString("yyyy-MM-dd");
                    ViewBag.PageSize = pageSize;

                    return View(model);
                }
                else if (getUser1 != null && getUser2 != null)
                {
                    itemList.AddRange(getUser1);
                    itemList.AddRange(getUser2);

                    itemList.AddRange(getUser1Approve);
                    itemList.AddRange(getUser2Approve);

                    foreach (var item in itemList)
                    {
                        if (item.Status == "Approve")
                        {
                            var updateData = _approvalRepository.GetAllApproval().Where(u => u.ApprovalId == item.ApprovalId).FirstOrDefault();

                            updateData.RemainingDay = 0;

                            _applicationDbContext.Approvals.Update(updateData);
                            _applicationDbContext.SaveChanges();
                        }
                        else
                        {
                            var remainingDay = DateTimeOffset.Now.Date - item.CreateDateTime.Date;
                            var updateData = _approvalRepository.GetAllApproval().Where(u => u.ApprovalId == item.ApprovalId).FirstOrDefault();

                            if (updateData.RemainingDay != 0)
                            {
                                updateData.RemainingDay = item.ExpiredDay - remainingDay.Days;

                                _applicationDbContext.Approvals.Update(updateData);
                                _applicationDbContext.SaveChanges();
                            }
                        }                            
                    }

                    var model = new Pagination<ApprovalPurchaseRequest>
                    {
                        Items = itemList,
                        TotalCount = itemList.Count,
                        PageSize = pageSize,
                        CurrentPage = page,
                    };

                    // Sertakan semua parameter untuk pagination
                    ViewBag.FilterOptions = filterOptions;
                    ViewBag.StartDateParam = startDate?.ToString("yyyy-MM-dd");
                    ViewBag.EndDateParam = endDate?.ToString("yyyy-MM-dd");
                    ViewBag.PageSize = pageSize;

                    return View(model);
                }
                else if (getUser1 != null)
                {
                    itemList.AddRange(getUser1);

                    itemList.AddRange(getUser1Approve);

                    foreach (var item in itemList)
                    {
                        if (item.Status == "Approve")
                        {
                            var updateData = _approvalRepository.GetAllApproval().Where(u => u.ApprovalId == item.ApprovalId).FirstOrDefault();

                            updateData.RemainingDay = 0;

                            _applicationDbContext.Approvals.Update(updateData);
                            _applicationDbContext.SaveChanges();
                        }
                        else
                        {
                            var remainingDay = DateTimeOffset.Now.Date - item.CreateDateTime.Date;
                            var updateData = _approvalRepository.GetAllApproval().Where(u => u.ApprovalId == item.ApprovalId).FirstOrDefault();

                            if (updateData.RemainingDay != 0)
                            {
                                updateData.RemainingDay = item.ExpiredDay - remainingDay.Days;

                                _applicationDbContext.Approvals.Update(updateData);
                                _applicationDbContext.SaveChanges();
                            }
                        }                            
                    }

                    var model = new Pagination<ApprovalPurchaseRequest>
                    {
                        Items = itemList,
                        TotalCount = itemList.Count,
                        PageSize = pageSize,
                        CurrentPage = page,
                    };

                    // Sertakan semua parameter untuk pagination
                    ViewBag.FilterOptions = filterOptions;
                    ViewBag.StartDateParam = startDate?.ToString("yyyy-MM-dd");
                    ViewBag.EndDateParam = endDate?.ToString("yyyy-MM-dd");
                    ViewBag.PageSize = pageSize;

                    return View(model);
                }
            }
            return View();
        }
        
        [HttpGet]
        [Authorize(Roles = "UpdateApprovalPurchaseRequest")]
        public async Task<ViewResult> DetailApprovalPurchaseRequest(Guid Id)
        {
            ViewBag.Active = "ApprovalPurchaseRequest";

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

            ApprovalPurchaseRequestViewModel viewModel = new ApprovalPurchaseRequestViewModel()
            {
                ApprovalId = Approval.ApprovalId,
                PurchaseRequestId = Approval.PurchaseRequestId,
                PurchaseRequestNumber = Approval.PurchaseRequestNumber,
                UserAccessId = Approval.UserAccessId,
                ExpiredDay = Approval.ExpiredDay,
                RemainingDay = Approval.RemainingDay,
                ExpiredDate = Approval.ExpiredDate,
                UserApproveId = Approval.UserApproveId,
                UserApprove = getUser.Email,
                ApprovalTime = "",
                ApproveBy = getUser.NamaUser,
                ApprovalDate = DateTime.Now,
                ApprovalStatusUser = Approval.ApprovalStatusUser,
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
                    Supplier = item.Supplier,
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
        [Authorize(Roles = "UpdateApprovalPurchaseRequest")]
        public async Task<IActionResult> DetailApprovalPurchaseRequest(ApprovalPurchaseRequestViewModel viewModel)
        {
            ViewBag.Active = "ApprovalPurchaseRequest";

            if (ModelState.IsValid)
            {
                var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();                

                var approval = await _approvalRepository.GetApprovalByIdNoTracking(viewModel.ApprovalId);
                var checkPR = _purchaseRequestRepository.GetAllPurchaseRequest().Where(c => c.PurchaseRequestNumber == viewModel.PurchaseRequestNumber).FirstOrDefault();
                var diffDate = DateTimeOffset.Now.Date - approval.CreateDateTime.Date;                

                if (checkPR != null)
                {
                    if (approval.ApprovalStatusUser == "User1" && checkPR.PurchaseRequestNumber == approval.PurchaseRequestNumber)
                    {
                        var updateStatusUser1 = _approvalRepository.GetAllApproval().Where(c => c.ApprovalStatusUser == "User1" && c.PurchaseRequestId == viewModel.PurchaseRequestId).FirstOrDefault();
                        if (updateStatusUser1.Status == "Waiting Approval")
                        {
                            updateStatusUser1.Status = viewModel.Status;
                            updateStatusUser1.ApprovalDate = DateTimeOffset.Now;
                            updateStatusUser1.ApproveBy = getUser.NamaUser;
                            updateStatusUser1.ApprovalTime = diffDate.Days.ToString() + " Day";
                            updateStatusUser1.Message = viewModel.Message;

                            _applicationDbContext.Entry(updateStatusUser1).State = EntityState.Modified;
                            _applicationDbContext.SaveChanges();
                        }

                        if (viewModel.Status == "Approve")
                        {
                            var updateStatusUser2 = _approvalRepository.GetAllApproval().Where(c => c.ApprovalStatusUser == "User2" && c.PurchaseRequestId == viewModel.PurchaseRequestId).FirstOrDefault();
                            if (updateStatusUser2 != null)
                            {
                                updateStatusUser2.Status = "User1Approve";

                                _applicationDbContext.Entry(updateStatusUser2).State = EntityState.Modified;
                                _applicationDbContext.SaveChanges();
                            }

                            checkPR.ApproveStatusUser1 = viewModel.Status;
                            checkPR.MessageApprove1 = viewModel.Message;

                            _applicationDbContext.Entry(checkPR).State = EntityState.Modified;
                            _applicationDbContext.SaveChanges();
                        }
                        else
                        {
                            checkPR.ApproveStatusUser1 = viewModel.Status;
                            checkPR.MessageApprove1 = viewModel.Message;

                            _applicationDbContext.Entry(checkPR).State = EntityState.Modified;
                            _applicationDbContext.SaveChanges();
                        }
                    }
                    else if (approval.ApprovalStatusUser == "User2" && checkPR.PurchaseRequestNumber == approval.PurchaseRequestNumber)
                    {
                        var updateStatusUser2 = _approvalRepository.GetAllApproval().Where(c => c.ApprovalStatusUser == "User2" && c.PurchaseRequestId == viewModel.PurchaseRequestId).FirstOrDefault();
                        if (updateStatusUser2.Status == "User1Approve")
                        {
                            updateStatusUser2.Status = viewModel.Status;
                            updateStatusUser2.ApprovalDate = DateTimeOffset.Now;
                            updateStatusUser2.ApproveBy = getUser.NamaUser;
                            updateStatusUser2.ApprovalTime = diffDate.Days.ToString() + " Day";
                            updateStatusUser2.Message = viewModel.Message;

                            _applicationDbContext.Entry(updateStatusUser2).State = EntityState.Modified;
                            _applicationDbContext.SaveChanges();
                        }

                        if (viewModel.Status == "Approve")
                        {
                            var updateStatusUser3 = _approvalRepository.GetAllApproval().Where(c => c.ApprovalStatusUser == "User3" && c.PurchaseRequestId == viewModel.PurchaseRequestId).FirstOrDefault();
                            if (updateStatusUser3 != null)
                            {
                                updateStatusUser3.Status = "User2Approve";

                                _applicationDbContext.Entry(updateStatusUser3).State = EntityState.Modified;
                                _applicationDbContext.SaveChanges();
                            }
                            checkPR.ApproveStatusUser2 = viewModel.Status;
                            checkPR.MessageApprove2 = viewModel.Message;

                            _applicationDbContext.Entry(checkPR).State = EntityState.Modified;
                            _applicationDbContext.SaveChanges();
                        }
                        else
                        {
                            checkPR.ApproveStatusUser2 = viewModel.Status;
                            checkPR.MessageApprove2 = viewModel.Message;

                            _applicationDbContext.Entry(checkPR).State = EntityState.Modified;
                            _applicationDbContext.SaveChanges();
                        }
                    }
                    else if (approval.ApprovalStatusUser == "User3" && checkPR.PurchaseRequestNumber == approval.PurchaseRequestNumber)
                    {
                        var updateStatusUser3 = _approvalRepository.GetAllApproval().Where(c => c.ApprovalStatusUser == "User3" && c.PurchaseRequestId == viewModel.PurchaseRequestId).FirstOrDefault();
                        if (updateStatusUser3.Status == "User2Approve")
                        {
                            updateStatusUser3.Status = viewModel.Status;
                            updateStatusUser3.ApprovalDate = DateTimeOffset.Now;
                            updateStatusUser3.ApproveBy = getUser.NamaUser;
                            updateStatusUser3.ApprovalTime = diffDate.Days.ToString() + " Day";
                            updateStatusUser3.Message = viewModel.Message;

                            _applicationDbContext.Entry(updateStatusUser3).State = EntityState.Modified;
                            _applicationDbContext.SaveChanges();
                        }

                        checkPR.ApproveStatusUser3 = viewModel.Status;
                        checkPR.MessageApprove3 = viewModel.Message;

                        _applicationDbContext.Entry(checkPR).State = EntityState.Modified;
                        _applicationDbContext.SaveChanges();
                    }

                    //Jika semua sudah Approve langsung Generate Purchase Order
                    if (checkPR.ApproveStatusUser1 == "Approve" && checkPR.ApproveStatusUser2 == "Approve" && checkPR.ApproveStatusUser3 == "Approve")
                    {
                        checkPR.Status = viewModel.Status;

                        _applicationDbContext.Entry(checkPR).State = EntityState.Modified;
                        _applicationDbContext.SaveChanges();

                        //Proses Generate Purchase Order
                        var po = new PurchaseOrder
                        {
                            CreateDateTime = DateTimeOffset.Now,
                            CreateBy = checkPR.CreateBy,
                            PurchaseRequestId = checkPR.PurchaseRequestId,
                            PurchaseRequestNumber = checkPR.PurchaseRequestNumber,
                            UserAccessId = checkPR.CreateBy.ToString(),
                            ExpiredDate = checkPR.ExpiredDate,
                            UserApprove1Id = checkPR.UserApprove1Id,
                            UserApprove2Id = checkPR.UserApprove2Id,
                            UserApprove3Id = checkPR.UserApprove3Id,
                            ApproveStatusUser1 = checkPR.ApproveStatusUser1,
                            ApproveStatusUser2 = checkPR.ApproveStatusUser2,
                            ApproveStatusUser3 = checkPR.ApproveStatusUser3,
                            TermOfPaymentId = checkPR.TermOfPaymentId,
                            Status = "In Order",
                            QtyTotal = checkPR.QtyTotal,
                            GrandTotal = Math.Truncate(checkPR.GrandTotal),
                            Note = checkPR.Note
                        };

                        var ItemsList = new List<PurchaseOrderDetail>();

                        foreach (var item in checkPR.PurchaseRequestDetails)
                        {
                            ItemsList.Add(new PurchaseOrderDetail
                            {
                                CreateDateTime = DateTimeOffset.Now,
                                CreateBy = new Guid(getUser.Id),
                                ProductNumber = item.ProductNumber,
                                ProductName = item.ProductName,
                                Supplier = item.Supplier,
                                Measurement = item.Measurement,
                                Qty = item.Qty,
                                Price = Math.Truncate(item.Price),
                                Discount = item.Discount,
                                SubTotal = Math.Truncate(item.SubTotal)
                            });
                        }

                        po.PurchaseOrderDetails = ItemsList;

                        var dateNow = DateTimeOffset.Now;
                        var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

                        var lastCode = _purchaseOrderRepository.GetAllPurchaseOrder().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.PurchaseOrderNumber).FirstOrDefault();
                        if (lastCode == null)
                        {
                            po.PurchaseOrderNumber = "PO" + setDateNow + "0001";
                        }
                        else
                        {
                            var lastCodeTrim = lastCode.PurchaseOrderNumber.Substring(2, 6);

                            if (lastCodeTrim != setDateNow)
                            {
                                po.PurchaseOrderNumber = "PO" + setDateNow + "0001";
                            }
                            else
                            {
                                po.PurchaseOrderNumber = "PO" + setDateNow + (Convert.ToInt32(lastCode.PurchaseOrderNumber.Substring(9, lastCode.PurchaseOrderNumber.Length - 9)) + 1).ToString("D4");
                            }
                        }

                        //Update Status PR Menjadi Nomor PO
                        var updatePurchaseRequest = _purchaseRequestRepository.GetAllPurchaseRequest().Where(c => c.PurchaseRequestId == viewModel.PurchaseRequestId).FirstOrDefault();
                        if (updatePurchaseRequest != null)
                        {
                            {
                                updatePurchaseRequest.Status = po.PurchaseOrderNumber;
                            };
                            _applicationDbContext.Entry(updatePurchaseRequest).State = EntityState.Modified;
                            _applicationDbContext.SaveChanges();
                        }

                        _purchaseOrderRepository.Tambah(po);

                        _approvalRepository.Update(approval);
                        _applicationDbContext.SaveChanges();

                        TempData["SuccessMessage"] = "Approve And Success Create Purchase Order";
                        return RedirectToAction("Index", "ApprovalPurchaseRequest");
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Update Success";
                        return RedirectToAction("Index", "ApprovalPurchaseRequest");
                    }
                }                
            }

            return View();
        }        
    }
}
