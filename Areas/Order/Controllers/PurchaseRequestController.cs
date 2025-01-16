﻿using FastReport.Data;
using FastReport.Export.PdfSimple;
using FastReport.Web;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PurchasingSystem.Areas.MasterData.Models;
using PurchasingSystem.Areas.MasterData.Repositories;
using PurchasingSystem.Areas.Order.Models;
using PurchasingSystem.Areas.Order.Repositories;
using PurchasingSystem.Areas.Order.ViewModels;
using PurchasingSystem.Data;
using PurchasingSystem.Hubs;
using PurchasingSystem.Models;
using PurchasingSystem.Repositories;
using QRCoder;
using System.Data;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace PurchasingSystem.Areas.Order.Controllers
{
    [Area("Order")]
    [Route("Order/[Controller]/[Action]")]
    public class PurchaseRequestController : Controller
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

        private readonly IDataProtector _protector;
        private readonly UrlMappingService _urlMappingService;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;


        public PurchaseRequestController(
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

            IDataProtectionProvider provider,
            UrlMappingService urlMappingService,
            IHostingEnvironment hostingEnvironment,
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

            _protector = provider.CreateProtector("UrlProtector");
            _urlMappingService = urlMappingService;
            _hostingEnvironment = hostingEnvironment;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
        }

        public JsonResult LoadProduk(Guid Id)
        {
            var produk = _applicationDbContext.Products.Include(p => p.Supplier).Include(s => s.Measurement).Include(d => d.Discount).Where(p => p.ProductId == Id).FirstOrDefault();
            return new JsonResult(produk);
        }        

        public JsonResult LoadPosition1(Guid Id)
        {
            var position = _applicationDbContext.Positions.Where(p => p.DepartmentId == Id).ToList();
            return Json(new SelectList(position, "PositionId", "PositionName"));
        }

        public JsonResult LoadPosition2(Guid Id)
        {
            var position = _applicationDbContext.Positions.Where(p => p.DepartmentId == Id).ToList();
            return Json(new SelectList(position, "PositionId", "PositionName"));
        }

        public JsonResult LoadPosition3(Guid Id)
        {
            var position = _applicationDbContext.Positions.Where(p => p.DepartmentId == Id).ToList();
            return Json(new SelectList(position, "PositionId", "PositionName"));
        }

        public JsonResult LoadUser1(Guid Id)
        {
            var user = _applicationDbContext.UserActives.Where(p => p.PositionId == Id).ToList();
            return Json(new SelectList(user, "UserActiveId", "FullName"));
        }
        public JsonResult LoadUser2(Guid Id)
        {
            var user = _applicationDbContext.UserActives.Where(p => p.PositionId == Id).ToList();
            return Json(new SelectList(user, "UserActiveId", "FullName"));
        }
        public JsonResult LoadUser3(Guid Id)
        {
            var user = _applicationDbContext.UserActives.Where(p => p.PositionId == Id).ToList();
            return Json(new SelectList(user, "UserActiveId", "FullName"));
        }

        public async Task<IActionResult> GetProductsPaged(string term, int page = 1, int pageSize = 10)
        {
            // Mulai query, include relasi Supplier
            var query = _applicationDbContext.Products
                .Include(p => p.Supplier)
                .AsQueryable();

            // Filter pencarian (jika user ketik di Select2)
            if (!string.IsNullOrWhiteSpace(term))
            {
                // Misal: filter by product name
                query = query.Where(p => p.ProductName.Contains(term) || p.Supplier.SupplierName.Contains(term));
            }

            // Total data
            int totalCount = await query.CountAsync();

            // Paging
            query = query.OrderBy(p => p.ProductName)
                         .Skip((page - 1) * pageSize)
                         .Take(pageSize);

            var items = await query.ToListAsync();

            // Hasil: di Select2 butuh { id, text }
            // "text" kita isi gabungan ProductName + '|' + SupplierName
            var results = items.Select(p => new {
                id = p.ProductId,
                text = p.ProductName + " | " + p.Supplier.SupplierName
            });

            bool more = (page * pageSize) < totalCount;

            var response = new
            {
                results = results,
                pagination = new
                {
                    more = more
                }
            };

            return Ok(response);
        }
       
        [HttpGet]
        [Authorize(Roles = "ReadPurchaseRequest")]
        public async Task<IActionResult> Index(string filterOptions = "", string searchTerm = "", DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, int page = 1, int pageSize = 10)
        {
            ViewBag.Active = "PurchaseRequest";
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
            var getUserActive = _userActiveRepository.GetAllUser().Where(c => c.UserActiveCode == getUserLogin.KodeUser).FirstOrDefault();

            if (getUserLogin.Email == "superadmin@admin.com")
            {
                var data = await _purchaseRequestRepository.GetAllPurchaseRequestPageSize(searchTerm, page, pageSize, startDate, endDate);
                
                var model = new Pagination<PurchaseRequest>
                {
                    Items = data.purchaseRequests,
                    TotalCount = data.totalCountPurchaseRequests,
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
                var data = await _purchaseRequestRepository.GetAllPurchaseRequestPageSize(searchTerm, page, pageSize, startDate, endDate);

                // Ambil data PurchaseRequest khusus user login
                var userPurchaseRequests = data.purchaseRequests
                    .Where(u => u.CreateBy.ToString() == getUserLogin.Id)
                    .ToList();

                foreach (var item in data.purchaseRequests)
                {
                    if (item.Status != "Waiting Approval")
                    {
                        var updateData = _purchaseRequestRepository.GetAllPurchaseRequest().Where(u => u.PurchaseRequestId == item.PurchaseRequestId).FirstOrDefault();

                        updateData.RemainingDay = 0;

                        _applicationDbContext.PurchaseRequests.Update(updateData);
                        _applicationDbContext.SaveChanges();
                    }
                    else
                    {
                        var remainingDay = DateTimeOffset.Now.Date - item.CreateDateTime.Date;
                        var updateData = _purchaseRequestRepository.GetAllPurchaseRequest().Where(u => u.PurchaseRequestId == item.PurchaseRequestId).FirstOrDefault();

                        if (updateData.RemainingDay != 0)
                        {
                            updateData.RemainingDay = item.ExpiredDay - remainingDay.Days;

                            _applicationDbContext.PurchaseRequests.Update(updateData);
                            _applicationDbContext.SaveChanges();
                        }
                    }
                }

                var model = new Pagination<PurchaseRequest>
                {
                    Items = userPurchaseRequests,
                    TotalCount = userPurchaseRequests.Count,
                    PageSize = pageSize,
                    CurrentPage = page,
                };

                return View(model);
            }
        }
        
        [HttpGet]
        [Authorize(Roles = "CreatePurchaseRequest")]
        public async Task<IActionResult> CreatePurchaseRequest(string searchTerm)
        {
            ViewBag.Active = "PurchaseRequest";

            _signInManager.IsSignedIn(User);
            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
                      
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
        [Authorize(Roles = "CreatePurchaseRequest")]
        public async Task<IActionResult> CreatePurchaseRequest(PurchaseRequest model)
        {
            ViewBag.Active = "PurchaseRequest";

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

            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();            

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
       
        [HttpGet]
        [Authorize(Roles = "UpdatePurchaseRequest")]
        public async Task<IActionResult> DetailPurchaseRequest(Guid Id)
        {
            ViewBag.Active = "PurchaseRequest";
            
            ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
            ViewBag.TermOfPayment = new SelectList(await _termOfPaymentRepository.GetTermOfPayments(), "TermOfPaymentId", "TermOfPaymentName", SortOrder.Ascending);            
            ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
            ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);

            var purchaseRequest = await _purchaseRequestRepository.GetPurchaseRequestById(Id);

            if (purchaseRequest == null)
            {
                Response.StatusCode = 404;
                return View("PurchaseRequestNotFound", Id);
            }

            PurchaseRequest model = new PurchaseRequest
            {
                PurchaseRequestId = purchaseRequest.PurchaseRequestId,
                PurchaseRequestNumber = purchaseRequest.PurchaseRequestNumber,
                UserAccessId = purchaseRequest.UserAccessId,
                Department1Id = purchaseRequest.Department1Id,
                Position1Id = purchaseRequest.Position1Id,
                UserApprove1Id = purchaseRequest.UserApprove1Id,
                ApproveStatusUser1 = purchaseRequest.ApproveStatusUser1,
                Department2Id = purchaseRequest.Department2Id,
                Position2Id = purchaseRequest.Position2Id,
                UserApprove2Id = purchaseRequest.UserApprove2Id,
                ApproveStatusUser2 = purchaseRequest.ApproveStatusUser2,
                Department3Id = purchaseRequest.Department3Id,
                Position3Id = purchaseRequest.Position3Id,
                UserApprove3Id = purchaseRequest.UserApprove3Id,
                ApproveStatusUser3 = purchaseRequest.ApproveStatusUser3,
                ExpiredDay = purchaseRequest.ExpiredDay,
                RemainingDay = purchaseRequest.RemainingDay,
                ExpiredDate = purchaseRequest.ExpiredDate,
                TermOfPaymentId = purchaseRequest.TermOfPaymentId,
                Status = purchaseRequest.Status,
                QtyTotal = purchaseRequest.QtyTotal,
                GrandTotal = Math.Truncate(purchaseRequest.GrandTotal),
                Note = purchaseRequest.Note,
                MessageApprove1 = purchaseRequest.MessageApprove1,
                MessageApprove2 = purchaseRequest.MessageApprove2,
                MessageApprove3 = purchaseRequest.MessageApprove3             
            };

            var ItemsList = new List<PurchaseRequestDetail>();

            foreach (var item in purchaseRequest.PurchaseRequestDetails)
            {
                ItemsList.Add(new PurchaseRequestDetail
                {
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

            model.PurchaseRequestDetails = ItemsList;
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "UpdatePurchaseRequest")]
        public async Task<IActionResult> DetailPurchaseRequest(PurchaseRequest model)
        {
            ViewBag.Active = "PurchaseRequest";

            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            if (ModelState.IsValid)
            {
                var purchaseRequest = _purchaseRequestRepository.GetAllPurchaseRequest().Where(p => p.PurchaseRequestNumber == model.PurchaseRequestNumber).FirstOrDefault();
                var approval = _approvalRepository.GetAllApproval().Where(p => p.PurchaseRequestNumber == model.PurchaseRequestNumber).ToList();               

                if (purchaseRequest != null)
                {
                    if (approval != null)
                    {
                        foreach (var item in approval)
                        {
                            var itemApproval = approval.Where(o => o.PurchaseRequestId == item.PurchaseRequestId).FirstOrDefault();
                            if (itemApproval != null)
                            {
                                item.Note = model.Note;

                                _applicationDbContext.Entry(itemApproval).State = EntityState.Modified;
                                _applicationDbContext.SaveChanges();
                            }
                            else
                            {                                
                                ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
                                ViewBag.TermOfPayment = new SelectList(await _termOfPaymentRepository.GetTermOfPayments(), "TermOfPaymentId", "TermOfPaymentName", SortOrder.Ascending);                                
                                ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
                                ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
                                TempData["WarningMessage"] = "Name " + item.ApproveBy + " Not Found !!!";
                                return View(model);
                            }
                        }

                        purchaseRequest.UpdateDateTime = DateTimeOffset.Now;
                        purchaseRequest.UpdateBy = new Guid(getUser.Id);                        
                        purchaseRequest.UserApprove1Id = model.UserApprove1Id;
                        purchaseRequest.UserApprove2Id = model.UserApprove2Id;
                        purchaseRequest.UserApprove3Id = model.UserApprove3Id;
                        purchaseRequest.TermOfPaymentId = model.TermOfPaymentId;                                              
                        purchaseRequest.QtyTotal = model.QtyTotal;
                        purchaseRequest.GrandTotal = model.GrandTotal;
                        purchaseRequest.ExpiredDate = model.ExpiredDate;
                        purchaseRequest.MessageApprove1 = model.MessageApprove1;
                        purchaseRequest.MessageApprove2 = model.MessageApprove2;
                        purchaseRequest.MessageApprove3 = model.MessageApprove3;
                        purchaseRequest.Note = model.Note;
                        purchaseRequest.PurchaseRequestDetails = model.PurchaseRequestDetails;                        

                        _purchaseRequestRepository.Update(purchaseRequest);

                        TempData["SuccessMessage"] = "Number " + model.PurchaseRequestNumber + " Changes saved";
                        return Json(new { redirectToUrl = Url.Action("Index", "PurchaseRequest") });
                    }
                    else
                    {                        
                        ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
                        ViewBag.TermOfPayment = new SelectList(await _termOfPaymentRepository.GetTermOfPayments(), "TermOfPaymentId", "TermOfPaymentName", SortOrder.Ascending);                        
                        ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
                        ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
                        TempData["WarningMessage"] = "Number " + model.PurchaseRequestNumber + " Not Found !!!";
                        return View(model);
                    }
                }
                else
                {                    
                    ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
                    ViewBag.TermOfPayment = new SelectList(await _termOfPaymentRepository.GetTermOfPayments(), "TermOfPaymentId", "TermOfPaymentName", SortOrder.Ascending);                    
                    ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
                    ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
                    TempData["WarningMessage"] = "Number " + model.PurchaseRequestNumber + " Already exists !!!";
                    return View(model);
                }
            }            
            ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
            ViewBag.TermOfPayment = new SelectList(await _termOfPaymentRepository.GetTermOfPayments(), "TermOfPaymentId", "TermOfPaymentName", SortOrder.Ascending);            
            ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
            ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
            TempData["WarningMessage"] = "Number " + model.PurchaseRequestNumber + " Failed saved";
            return Json(new { redirectToUrl = Url.Action("Index", "PurchaseRequest") });
        }

        [Authorize(Roles = "PreviewPurchaseRequest")]
        public async Task<IActionResult> PreviewPurchaseRequest(Guid Id)
        {
            var purchaseRequest = await _purchaseRequestRepository.GetPurchaseRequestById(Id);

            var CreateDate = purchaseRequest.CreateDateTime.ToString("dd MMMM yyyy");
            var PrNumber = purchaseRequest.PurchaseRequestNumber;
            var CreateBy = purchaseRequest.ApplicationUser.NamaUser;
            var UserApprove1 = purchaseRequest.UserApprove1.FullName;
            var UserApprove2 = purchaseRequest.UserApprove2.FullName;
            var UserApprove3 = purchaseRequest.UserApprove3.FullName;
            var TermOfPayment = purchaseRequest.TermOfPayment.TermOfPaymentName;
            var Note = purchaseRequest.Note;
            var GrandTotal = purchaseRequest.GrandTotal;
            var Tax = (GrandTotal / 100) * 11;
            var GrandTotalAfterTax = (GrandTotal + Tax);

            // Path logo untuk QR code
            var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.png");

            // Generate QR Code dengan logo
            var qrCodeImage = GenerateQRCodeWithLogo(PrNumber, logoPath);

            // Simpan QR Code ke dalam MemoryStream sebagai PNG
            using var qrCodeStream = new MemoryStream();
            qrCodeImage.Save(qrCodeStream, System.Drawing.Imaging.ImageFormat.Png);
            qrCodeStream.Position = 0;

            WebReport web = new WebReport();
            var path = $"{_webHostEnvironment.WebRootPath}\\Reporting\\PurchaseRequest.frx";
            web.Report.Load(path);

            // Tambahkan data QR Code sebagai PictureObject
            var pictureObject = web.Report.FindObject("Picture1") as FastReport.PictureObject;
            if (pictureObject != null)
            {
                pictureObject.Image = Image.FromStream(qrCodeStream);
            }

            var msSqlDataConnection = new MsSqlDataConnection();
            msSqlDataConnection.ConnectionString = _configuration.GetConnectionString("DefaultConnection");
            var Conn = msSqlDataConnection.ConnectionString;            

            web.Report.SetParameterValue("Conn", Conn);
            web.Report.SetParameterValue("PurchaseRequestId", Id.ToString());
            web.Report.SetParameterValue("PrNumber", PrNumber);
            web.Report.SetParameterValue("CreateDate", CreateDate);
            web.Report.SetParameterValue("CreateBy", CreateBy);
            web.Report.SetParameterValue("UserApprove1", UserApprove1);
            web.Report.SetParameterValue("UserApprove2", UserApprove2);
            web.Report.SetParameterValue("UserApprove3", UserApprove3);
            web.Report.SetParameterValue("TermOfPayment", TermOfPayment);
            web.Report.SetParameterValue("Note", Note);
            web.Report.SetParameterValue("GrandTotal", GrandTotal);
            web.Report.SetParameterValue("Tax", Tax);
            web.Report.SetParameterValue("GrandTotalAfterTax", GrandTotalAfterTax);

            Stream stream = new MemoryStream();

            web.Report.Prepare();
            web.Report.Export(new PDFSimpleExport(), stream);
            stream.Position = 0;

            return File(stream, "application/pdf");
        }

        [Authorize(Roles = "DownloadPurchaseRequest")]
        public async Task<IActionResult> DownloadPurchaseRequest(Guid Id)
        {
            var purchaseRequest = await _purchaseRequestRepository.GetPurchaseRequestById(Id);

            var CreateDate = purchaseRequest.CreateDateTime.ToString("dd MMMM yyyy");
            var PrNumber = purchaseRequest.PurchaseRequestNumber;
            var CreateBy = purchaseRequest.ApplicationUser.NamaUser;
            var UserApprove1 = purchaseRequest.UserApprove1.FullName;
            var UserApprove2 = purchaseRequest.UserApprove2.FullName;
            var UserApprove3 = purchaseRequest.UserApprove3.FullName;
            var TermOfPayment = purchaseRequest.TermOfPayment.TermOfPaymentName;
            var Note = purchaseRequest.Note;
            var GrandTotal = purchaseRequest.GrandTotal;
            var Tax = (GrandTotal / 100) * 11;
            var GrandTotalAfterTax = (GrandTotal + Tax);

            // Path logo untuk QR code
            var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.png");

            // Generate QR Code dengan logo
            var qrCodeImage = GenerateQRCodeWithLogo(PrNumber, logoPath);

            // Simpan QR Code ke dalam MemoryStream sebagai PNG
            using var qrCodeStream = new MemoryStream();
            qrCodeImage.Save(qrCodeStream, System.Drawing.Imaging.ImageFormat.Png);
            qrCodeStream.Position = 0;

            WebReport web = new WebReport();
            var path = $"{_webHostEnvironment.WebRootPath}\\Reporting\\PurchaseRequest.frx";
            web.Report.Load(path);

            // Tambahkan data QR Code sebagai PictureObject
            var pictureObject = web.Report.FindObject("Picture1") as FastReport.PictureObject;
            if (pictureObject != null)
            {
                pictureObject.Image = Image.FromStream(qrCodeStream);
            }

            var msSqlDataConnection = new MsSqlDataConnection();
            msSqlDataConnection.ConnectionString = _configuration.GetConnectionString("DefaultConnection");
            var Conn = msSqlDataConnection.ConnectionString;

            web.Report.SetParameterValue("Conn", Conn);
            web.Report.SetParameterValue("PurchaseRequestId", Id.ToString());
            web.Report.SetParameterValue("PrNumber", PrNumber);
            web.Report.SetParameterValue("CreateDate", CreateDate);
            web.Report.SetParameterValue("CreateBy", CreateBy);
            web.Report.SetParameterValue("UserApprove1", UserApprove1);
            web.Report.SetParameterValue("UserApprove2", UserApprove2);
            web.Report.SetParameterValue("UserApprove3", UserApprove3);
            web.Report.SetParameterValue("TermOfPayment", TermOfPayment);
            web.Report.SetParameterValue("Note", Note);
            web.Report.SetParameterValue("GrandTotal", GrandTotal);
            web.Report.SetParameterValue("Tax", Tax);
            web.Report.SetParameterValue("GrandTotalAfterTax", GrandTotalAfterTax);

            web.Report.Prepare();

            Stream stream = new MemoryStream();
            web.Report.Export(new PDFSimpleExport(), stream);
            stream.Position = 0;

            return File(stream, "application/zip", (PrNumber + ".pdf"));
        }

        private Bitmap GenerateQRCodeWithLogo(string text, string logoPath)
        {
            if (!System.IO.File.Exists(logoPath))
            {
                throw new FileNotFoundException("Logo file not found", logoPath);
            }

            // Step 1: Generate QR code as byte array using PngByteQRCode
            using var qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.H);
            using var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(4);

            // Step 2: Load QR code byte array into Bitmap
            Bitmap qrBitmap;
            using (var ms = new MemoryStream(qrCodeBytes))
            {
                qrBitmap = new Bitmap(ms);
            }

            // Step 3: Ensure QR code is in a writable pixel format
            Bitmap writableQrBitmap = new Bitmap(qrBitmap.Width, qrBitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(writableQrBitmap))
            {
                graphics.DrawImage(qrBitmap, 0, 0);
            }

            // Step 4: Load logo and add it to the QR code
            using var logoBitmap = new Bitmap(logoPath);
            using (var graphics = Graphics.FromImage(writableQrBitmap))
            {
                int logoSize = (writableQrBitmap.Width + 125) / 5; // Logo size (20% of QR code size)
                int x = (writableQrBitmap.Width - logoSize) / 2;
                int y = (writableQrBitmap.Height - logoSize) / 2;
                graphics.DrawImage(logoBitmap, new Rectangle(x, y, logoSize, logoSize));
            }

            return writableQrBitmap;
        }
    }
}
