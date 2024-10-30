﻿using FastReport.Data;
using FastReport.Export.PdfSimple;
using FastReport.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PurchasingSystemStaging.Areas.MasterData.Models;
using PurchasingSystemStaging.Areas.MasterData.Repositories;
using PurchasingSystemStaging.Areas.Order.Repositories;
using PurchasingSystemStaging.Areas.Transaction.Models;
using PurchasingSystemStaging.Areas.Transaction.Repositories;
using PurchasingSystemStaging.Areas.Transaction.ViewModels;
using PurchasingSystemStaging.Areas.Warehouse.Models;
using PurchasingSystemStaging.Areas.Warehouse.Repositories;
using PurchasingSystemStaging.Data;
using PurchasingSystemStaging.Models;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace PurchasingSystemStaging.Areas.Transaction.Controllers
{
    [Area("Transaction")]
    [Route("Transaction/[Controller]/[Action]")]
    public class UnitRequestController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUnitRequestRepository _unitRequestRepository;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IProductRepository _productRepository;
        private readonly ITermOfPaymentRepository _termOfPaymentRepository;
        private readonly IApprovalUnitRequestRepository _approvalRequestRepository;
        private readonly IUnitLocationRepository _unitLocationRepository;
        private readonly IWarehouseLocationRepository _warehouseLocationRepository;
        private readonly IUnitOrderRepository _UnitOrderRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IPositionRepository _positionRepository;

        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;


        public UnitRequestController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IUnitRequestRepository UnitRequestRepository,
            IUserActiveRepository userActiveRepository,
            IProductRepository productRepository,
            ITermOfPaymentRepository termOfPaymentRepository,
            IApprovalUnitRequestRepository approvalRequestRepository,
            IUnitLocationRepository unitLocationRepository,
            IWarehouseLocationRepository warehouseLocationRepository,
            IUnitOrderRepository UnitOrderRepository,
            IDepartmentRepository departmentRepository,
            IPositionRepository positionRepository,

            IHostingEnvironment hostingEnvironment,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _unitRequestRepository = UnitRequestRepository;
            _userActiveRepository = userActiveRepository;
            _productRepository = productRepository;
            _termOfPaymentRepository = termOfPaymentRepository;
            _approvalRequestRepository = approvalRequestRepository;
            _unitLocationRepository = unitLocationRepository;
            _warehouseLocationRepository = warehouseLocationRepository;
            _UnitOrderRepository = UnitOrderRepository;
            _departmentRepository = departmentRepository;
            _positionRepository = positionRepository;

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

        public JsonResult LoadUser1(Guid Id)
        {
            var user = _applicationDbContext.UserActives.Where(p => p.PositionId == Id).ToList();
            return Json(new SelectList(user, "UserActiveId", "FullName"));
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.Active = "UnitRequest";
            var data = _unitRequestRepository.GetAllUnitRequest();
            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> Index(DateTime tglAwalPencarian, DateTime tglAkhirPencarian)
        {
            ViewBag.Active = "UnitRequest";
            ViewBag.tglAwalPencarian = tglAwalPencarian.ToString("dd MMMM yyyy");
            ViewBag.tglAkhirPencarian = tglAkhirPencarian.ToString("dd MMMM yyyy");

            var data = _unitRequestRepository.GetAllUnitRequest().Where(r => r.CreateDateTime.Date >= tglAwalPencarian && r.CreateDateTime.Date <= tglAkhirPencarian).ToList();
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> CreateUnitRequest()
        {
            ViewBag.Active = "UnitRequest";

            _signInManager.IsSignedIn(User);
            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            ViewBag.UnitLocation = new SelectList(await _unitLocationRepository.GetUnitLocations(), "UnitLocationId", "UnitLocationName", SortOrder.Ascending);            
            ViewBag.WarehouseLocation = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);            
            ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
            ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
            ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
            ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);

            UnitRequest UnitRequest = new UnitRequest()
            {
                UserAccessId = getUser.Id,
            };
            UnitRequest.UnitRequestDetails.Add(new UnitRequestDetail() { UnitRequestDetailId = Guid.NewGuid() });

            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _unitRequestRepository.GetAllUnitRequest().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.UnitRequestNumber).FirstOrDefault();
            if (lastCode == null)
            {
                UnitRequest.UnitRequestNumber = "REQ" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.UnitRequestNumber.Substring(2, 6);

                if (lastCodeTrim != setDateNow)
                {
                    UnitRequest.UnitRequestNumber = "REQ" + setDateNow + "0001";
                }
                else
                {
                    UnitRequest.UnitRequestNumber = "REQ" + setDateNow + (Convert.ToInt32(lastCode.UnitRequestNumber.Substring(9, lastCode.UnitRequestNumber.Length - 9)) + 1).ToString("D4");
                }
            }

            return View(UnitRequest);
        }


        [HttpPost]
        public async Task<IActionResult> CreateUnitRequest(UnitRequest model)
        {
            ViewBag.Active = "UnitRequest";

            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _unitRequestRepository.GetAllUnitRequest().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.UnitRequestNumber).FirstOrDefault();
            if (lastCode == null)
            {
                model.UnitRequestNumber = "REQ" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.UnitRequestNumber.Substring(3, 6);

                if (lastCodeTrim != setDateNow)
                {
                    model.UnitRequestNumber = "REQ" + setDateNow + "0001";
                }
                else
                {
                    model.UnitRequestNumber = "REQ" + setDateNow + (Convert.ToInt32(lastCode.UnitRequestNumber.Substring(9, lastCode.UnitRequestNumber.Length - 9)) + 1).ToString("D4");
                }
            }

            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            if (ModelState.IsValid)
            {
                var UnitRequest = new UnitRequest
                {
                    CreateDateTime = DateTime.Now,
                    CreateBy = new Guid(getUser.Id), //Convert Guid to String
                    UnitRequestId = model.UnitRequestId,
                    UnitRequestNumber = model.UnitRequestNumber,
                    UserAccessId = getUser.Id,
                    Department1Id = model.Department1Id,
                    Position1Id = model.Position1Id,
                    UserApprove1Id = model.UserApprove1Id,
                    ApproveStatusUser1 = model.ApproveStatusUser1,
                    UnitLocationId = model.UnitLocationId,                    
                    WarehouseLocationId = model.WarehouseLocationId,
                    QtyTotal = model.QtyTotal,
                    Status = model.Status,
                    Note = model.Note,
                    MessageApprove1 = model.MessageApprove1,
                };

                var ItemsList = new List<UnitRequestDetail>();

                foreach (var item in model.UnitRequestDetails)
                {
                    ItemsList.Add(new UnitRequestDetail
                    {
                        CreateDateTime = DateTime.Now,
                        CreateBy = new Guid(getUser.Id),
                        ProductNumber = item.ProductNumber,
                        ProductName = item.ProductName,
                        Measurement = item.Measurement,
                        Supplier = item.Supplier,
                        Qty = item.Qty,
                    });
                }

                UnitRequest.UnitRequestDetails = ItemsList;
                _unitRequestRepository.Tambah(UnitRequest);

                if (model.UserApprove1Id != null)
                {
                    var approval = new ApprovalUnitRequest
                    {
                        CreateDateTime = DateTime.Now,
                        CreateBy = new Guid(getUser.Id),
                        UnitRequestId = UnitRequest.UnitRequestId,
                        UnitRequestNumber = UnitRequest.UnitRequestNumber,
                        UserAccessId = getUser.Id.ToString(),
                        UnitLocationId = UnitRequest.UnitLocationId,
                        WarehouseLocation = UnitRequest.WarehouseLocation,
                        UserApproveId = UnitRequest.UserApprove1Id,
                        ApproveBy = "",
                        ApprovalTime = "",
                        ApprovalDate = DateTimeOffset.MinValue,
                        ApprovalStatusUser = "User1",
                        Status = UnitRequest.Status,
                        Note = UnitRequest.Note,
                        Message = UnitRequest.MessageApprove1
                    };
                    _approvalRequestRepository.Tambah(approval);
                }                

                TempData["SuccessMessage"] = "Number " + model.UnitRequestNumber + " Saved";
                return Json(new { redirectToUrl = Url.Action("Index", "UnitRequest") });
            }
            else
            {
                ViewBag.UnitLocation = new SelectList(await _unitLocationRepository.GetUnitLocations(), "UnitLocationId", "UnitLocationName", SortOrder.Ascending);
                ViewBag.WarehouseLocation = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);
                ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
                ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
                ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
                ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
                TempData["WarningMessage"] = "Please, input all data !";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> DetailUnitRequest(Guid Id)
        {
            ViewBag.Active = "UnitRequest";

            ViewBag.UnitLocation = new SelectList(await _unitLocationRepository.GetUnitLocations(), "UnitLocationId", "UnitLocationName", SortOrder.Ascending);
            ViewBag.WarehouseLocation = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);
            ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
            ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
            ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
            ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);

            var UnitRequest = await _unitRequestRepository.GetUnitRequestById(Id);

            if (UnitRequest == null)
            {
                Response.StatusCode = 404;
                return View("UnitRequestNotFound", Id);
            }

            UnitRequest model = new UnitRequest
            {
                UnitRequestId = UnitRequest.UnitRequestId,
                UnitRequestNumber = UnitRequest.UnitRequestNumber,
                UserAccessId = UnitRequest.UserAccessId,
                Department1Id = UnitRequest.Department1Id,
                Position1Id = UnitRequest.Position1Id,
                UserApprove1Id = UnitRequest.UserApprove1Id,
                ApproveStatusUser1 = UnitRequest.ApproveStatusUser1,
                UnitLocationId = UnitRequest.UnitLocationId,
                WarehouseLocationId = UnitRequest.WarehouseLocationId,
                QtyTotal = UnitRequest.QtyTotal,
                Status = UnitRequest.Status,
                Note = UnitRequest.Note,
                MessageApprove1 = UnitRequest.MessageApprove1,
            };

            var ItemsList = new List<UnitRequestDetail>();

            foreach (var item in UnitRequest.UnitRequestDetails)
            {
                ItemsList.Add(new UnitRequestDetail
                {
                    ProductNumber = item.ProductNumber,
                    ProductName = item.ProductName,
                    Measurement = item.Measurement,
                    Supplier = item.Supplier,
                    Qty = item.Qty,
                });
            }

            model.UnitRequestDetails = ItemsList;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DetailUnitRequest(UnitRequest model)
        {
            ViewBag.Active = "UnitRequest";

            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            if (ModelState.IsValid)
            {
                var UnitRequest = _unitRequestRepository.GetAllUnitRequest().Where(p => p.UnitRequestNumber == model.UnitRequestNumber).FirstOrDefault();
                var approval = _approvalRequestRepository.GetAllApprovalRequest().Where(p => p.UnitRequestNumber == model.UnitRequestNumber).ToList();

                if (UnitRequest != null)
                {
                    if (approval != null)
                    {
                        foreach (var item in approval)
                        {
                            var itemApproval = approval.Where(o => o.UnitRequestId == item.UnitRequestId).FirstOrDefault();
                            if (itemApproval != null)
                            {
                                item.Note = model.Note;

                                _applicationDbContext.Entry(itemApproval).State = EntityState.Modified;
                                _applicationDbContext.SaveChanges();
                            }
                            else
                            {
                                ViewBag.UnitLocation = new SelectList(await _unitLocationRepository.GetUnitLocations(), "UnitLocationId", "UnitLocationName", SortOrder.Ascending);
                                ViewBag.WarehouseLocation = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);
                                ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
                                ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
                                ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
                                ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
                                TempData["WarningMessage"] = "Name " + item.ApproveBy + " Not Found !!!";
                                return View(model);
                            }
                        }

                        UnitRequest.UpdateDateTime = DateTime.Now;
                        UnitRequest.UpdateBy = new Guid(getUser.Id);
                        UnitRequest.UnitLocationId = model.UnitLocationId;
                        UnitRequest.WarehouseLocationId = model.WarehouseLocationId;
                        UnitRequest.Department1Id = model.Department1Id;
                        UnitRequest.Position1Id = model.Position1Id;
                        UnitRequest.UserApprove1Id = model.UserApprove1Id;
                        UnitRequest.ApproveStatusUser1 = model.ApproveStatusUser1;
                        UnitRequest.QtyTotal = model.QtyTotal;
                        UnitRequest.Note = model.Note;
                        UnitRequest.MessageApprove1 = model.MessageApprove1;
                        UnitRequest.UnitRequestDetails = model.UnitRequestDetails;

                        _unitRequestRepository.Update(UnitRequest);

                        TempData["SuccessMessage"] = "Number " + model.UnitRequestNumber + " Changes saved";
                        return Json(new { redirectToUrl = Url.Action("Index", "UnitRequest") });
                    }
                    else
                    {
                        ViewBag.UnitLocation = new SelectList(await _unitLocationRepository.GetUnitLocations(), "UnitLocationId", "UnitLocationName", SortOrder.Ascending);
                        ViewBag.WarehouseLocation = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);
                        ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
                        ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
                        ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
                        ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
                        TempData["WarningMessage"] = "Number " + model.UnitRequestNumber + " Not Found !!!";
                        return View(model);
                    }
                }
                else
                {
                    ViewBag.UnitLocation = new SelectList(await _unitLocationRepository.GetUnitLocations(), "UnitLocationId", "UnitLocationName", SortOrder.Ascending);
                    ViewBag.WarehouseLocation = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);
                    ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
                    ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
                    ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
                    ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
                    TempData["WarningMessage"] = "Number " + model.UnitRequestNumber + " Already exists !!!";
                    return View(model);
                }
            }
            ViewBag.UnitLocation = new SelectList(await _unitLocationRepository.GetUnitLocations(), "UnitLocationId", "UnitLocationName", SortOrder.Ascending);
            ViewBag.WarehouseLocation = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);
            ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
            ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
            ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
            ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
            TempData["WarningMessage"] = "Number " + model.UnitRequestNumber + " Failed saved";
            return Json(new { redirectToUrl = Url.Action("Index", "UnitRequest") });
        }

        //[HttpGet]
        //public async Task<IActionResult> GenerateRequest(Guid Id)
        //{
        //    ViewBag.Active = "UnitRequest";

        //    ViewBag.UnitLocation = new SelectList(await _unitLocationRepository.GetUnitLocations(), "UnitLocationId", "UnitLocationName", SortOrder.Ascending);
        //    ViewBag.WarehouseLocation = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);
        //    ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
        //    ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
        //    ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
        //    ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);

        //    UnitRequest UnitRequest = _applicationDbContext.UnitRequests
        //        .Include(d => d.UnitRequestDetails)
        //        .Include(u => u.ApplicationUser)
        //        .Include(z => z.UnitLocation)
        //        .Include(t => t.WarehouseLocation)
        //        .Include(d1 => d1.Department1)
        //        .Include(p1 => p1.Position1)
        //        .Include(a1 => a1.UserApprove1)
        //        .Where(p => p.UnitRequestId == Id).FirstOrDefault();

        //    _signInManager.IsSignedIn(User);

        //    var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

        //    UnitOrder wr = new UnitOrder();

        //    var dateNow = DateTimeOffset.Now;
        //    var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

        //    var lastCode = _UnitOrderRepository.GetAllUnitOrder().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.UnitOrderNumber).FirstOrDefault();
        //    if (lastCode == null)
        //    {
        //        wr.UnitOrderNumber = "WRQ" + setDateNow + "0001";
        //    }
        //    else
        //    {
        //        var lastCodeTrim = lastCode.UnitOrderNumber.Substring(3, 6);

        //        if (lastCodeTrim != setDateNow)
        //        {
        //            wr.UnitOrderNumber = "WRQ" + setDateNow + "0001";
        //        }
        //        else
        //        {
        //            wr.UnitOrderNumber = "WRQ" + setDateNow + (Convert.ToInt32(lastCode.UnitOrderNumber.Substring(9, lastCode.UnitOrderNumber.Length - 9)) + 1).ToString("D4");
        //        }
        //    }

        //    ViewBag.UnitOrderNumber = wr.UnitOrderNumber;

        //    var getUnitReq = new UnitRequestViewModel()
        //    {
        //        UnitRequestId = UnitRequest.UnitRequestId,
        //        UnitRequestNumber = UnitRequest.UnitRequestNumber,
        //        UserAccessId = UnitRequest.UserAccessId,
        //        UnitLocationId = UnitRequest.UnitLocationId,
        //        WarehouseLocationId = UnitRequest.WarehouseLocationId,
        //        UserApprove1Id = UnitRequest.UserApprove1Id,
        //        ApproveStatusUser1 = UnitRequest.ApproveStatusUser1,
        //        QtyTotal = UnitRequest.QtyTotal,
        //        Status = UnitRequest.Status,
        //        Note = UnitRequest.Note,
        //        MessageApprove1 = UnitRequest.MessageApprove1
        //    };

        //    var ItemsList = new List<UnitRequestDetail>();

        //    foreach (var item in UnitRequest.UnitRequestDetails)
        //    {
        //        ItemsList.Add(new UnitRequestDetail
        //        {
        //            CreateDateTime = DateTime.Now,
        //            CreateBy = new Guid(getUser.Id),
        //            ProductNumber = item.ProductNumber,
        //            ProductName = item.ProductName,
        //            Supplier = item.Supplier,
        //            Measurement = item.Measurement,
        //            Qty = item.Qty
        //        });
        //    }

        //    getUnitReq.UnitRequestDetails = ItemsList;
        //    return View(getUnitReq);
        //}

        //[HttpPost]
        //public async Task<IActionResult> GenerateRequest(UnitRequest model, UnitRequestViewModel vm)
        //{
        //    ViewBag.Active = "UnitRequest";

        //    UnitRequest UnitRequest = await _unitRequestRepository.GetUnitRequestById(model.UnitRequestId);

        //    _signInManager.IsSignedIn(User);

        //    var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

        //    string getUnitOrderNumber = Request.Form["WRNumber"];

        //    var updateUnitRequest = _unitRequestRepository.GetAllUnitRequest().Where(c => c.UnitRequestId == model.UnitRequestId).FirstOrDefault();
        //    if (updateUnitRequest != null)
        //    {
        //        {
        //            updateUnitRequest.Status = "InProcess";
        //        };
        //        _applicationDbContext.Entry(updateUnitRequest).State = EntityState.Modified;
        //    }

        //    var newUnitOrder = new UnitOrder
        //    {
        //        CreateDateTime = DateTime.Now,
        //        CreateBy = new Guid(getUser.Id),
        //        UnitRequestId = UnitRequest.UnitRequestId,
        //        UnitRequestNumber = UnitRequest.UnitRequestNumber,
        //        UserAccessId = getUser.Id.ToString(),
        //        UnitLocationId = UnitRequest.UnitLocationId,
        //        WarehouseLocationId = UnitRequest.WarehouseLocationId,
        //        UserApprove1Id = UnitRequest.UserApprove1Id,
        //        ApproveStatusUser1 = UnitRequest.ApproveStatusUser1,
        //        Status = "Process",
        //        QtyTotal = UnitRequest.QtyTotal,
        //    };

        //    newUnitOrder.UnitOrderNumber = getUnitOrderNumber;

        //    var ItemsList = new List<UnitOrderDetail>();

        //    foreach (var item in UnitRequest.UnitRequestDetails)
        //    {
        //        ItemsList.Add(new UnitOrderDetail
        //        {
        //            CreateDateTime = DateTime.Now,
        //            CreateBy = new Guid(getUser.Id),
        //            ProductNumber = item.ProductNumber,
        //            ProductName = item.ProductName,
        //            Supplier = item.Supplier,
        //            Measurement = item.Measurement,
        //            Qty = item.Qty,
        //            Checked = item.Checked
        //        });
        //    }

        //    newUnitOrder.UnitOrderDetails = ItemsList;
        //    _UnitOrderRepository.Tambah(newUnitOrder);

        //    TempData["SuccessMessage"] = "Number " + newUnitOrder.UnitOrderNumber + " Saved";
        //    return RedirectToAction("Index", "UnitRequest");
        //}

        //public async Task<IActionResult> PrintUnitRequest(Guid Id)
        //{
            //var unitRequest = await _unitRequestRepository.GetUnitRequestById(Id);

            //var CreateDate = DateTime.Now.ToString("dd MMMM yyyy");
            //var ReqNumber = unitRequest.UnitRequestNumber;
            //var CreateBy = unitRequest.ApplicationUser.NamaUser;
            //var HeadUnit = unitRequest.UnitRequestManager.FullName;
            //var UnitLocation = unitRequest.UnitLocation.UnitLocationName;
            //var HeadWarehouse = unitRequest.WarehouseApproval.FullName;
            //var WarehouseLocation = unitRequest.WarehouseLocation.WarehouseLocationName;            
            //var Note = unitRequest.Note;
            //var QtyTotal = unitRequest.QtyTotal;

            //WebReport web = new WebReport();
            //var path = $"{_webHostEnvironment.WebRootPath}\\Reporting\\UnitRequest.frx";
            //web.Report.Load(path);

            //var msSqlDataConnection = new MsSqlDataConnection();
            //msSqlDataConnection.ConnectionString = _configuration.GetConnectionString("DefaultConnection");
            //var Conn = msSqlDataConnection.ConnectionString;

            //web.Report.SetParameterValue("Conn", Conn);
            //web.Report.SetParameterValue("UnitRequestId", Id.ToString());
            //web.Report.SetParameterValue("ReqNumber", ReqNumber);
            //web.Report.SetParameterValue("CreateDate", CreateDate);
            //web.Report.SetParameterValue("CreateBy", CreateBy);
            //web.Report.SetParameterValue("HeadUnit", HeadUnit);
            //web.Report.SetParameterValue("UnitLocation", UnitLocation);
            //web.Report.SetParameterValue("HeadWarehouse", HeadWarehouse);
            //web.Report.SetParameterValue("WarehouseLocation", WarehouseLocation);
            //web.Report.SetParameterValue("Note", Note);
            //web.Report.SetParameterValue("QtyTotal", QtyTotal);

            //web.Report.Prepare();
            //Stream stream = new MemoryStream();
            //web.Report.Export(new PDFSimpleExport(), stream);
            //stream.Position = 0;
            //return File(stream, "application/zip", (ReqNumber + ".pdf"));
        //}
    }
}
