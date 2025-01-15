using FastReport.Data;
using FastReport.Export.PdfSimple;
using FastReport.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PurchasingSystem.Areas.MasterData.Models;
using PurchasingSystem.Areas.MasterData.Repositories;
using PurchasingSystem.Areas.Order.Models;
using PurchasingSystem.Areas.Order.Repositories;
using PurchasingSystem.Areas.Transaction.Models;
using PurchasingSystem.Areas.Transaction.Repositories;
using PurchasingSystem.Areas.Transaction.ViewModels;
using PurchasingSystem.Areas.Warehouse.Models;
using PurchasingSystem.Areas.Warehouse.Repositories;
using PurchasingSystem.Data;
using PurchasingSystem.Models;
using PurchasingSystem.Repositories;
using QRCoder;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace PurchasingSystem.Areas.Transaction.Controllers
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

        private readonly IDataProtector _protector;
        private readonly UrlMappingService _urlMappingService;
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

        public JsonResult LoadUser1(Guid Id)
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
        [Authorize(Roles = "ReadUnitRequest")]
        public async Task<IActionResult> Index(string filterOptions = "", string searchTerm = "", DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, int page = 1, int pageSize = 10)
        {
            ViewBag.Active = "UnitRequest";
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

            var data = await _unitRequestRepository.GetAllUnitRequestPageSize(searchTerm, page, pageSize, startDate, endDate);

            var model = new Pagination<UnitRequest>
            {
                Items = data.unitRequests.Where(u => u.CreateBy.ToString() == getUserLogin.Id).ToList(),
                TotalCount = data.totalCountUnitRequests,
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
        
        [HttpGet]
        [Authorize(Roles = "CreateUnitRequest")]
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
        [Authorize(Roles = "CreateUnitRequest")]
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
                        WarehouseLocationId = UnitRequest.WarehouseLocationId,
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
        [Authorize(Roles = "UpdateUnitRequest")]
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
        [Authorize(Roles = "UpdateUnitRequest")]
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

        [Authorize(Roles = "PreviewUnitRequest")]
        public async Task<IActionResult> PreviewUnitRequest(Guid Id)
        {
            var unitRequest = await _unitRequestRepository.GetUnitRequestById(Id);

            var CreateDate = unitRequest.CreateDateTime.ToString("dd MMMM yyyy");
            var ReqNumber = unitRequest.UnitRequestNumber;
            var CreateBy = unitRequest.ApplicationUser.NamaUser;
            var UserApprove1 = unitRequest.UserApprove1.FullName;
            var UnitLocation = unitRequest.UnitLocation.UnitLocationName;
            var WarehouseLocation = unitRequest.WarehouseLocation.WarehouseLocationName;
            var Note = unitRequest.Note;
            var QtyTotal = unitRequest.QtyTotal;

            // Path logo untuk QR code
            var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.png");

            // Generate QR Code dengan logo
            var qrCodeImage = GenerateQRCodeWithLogo(ReqNumber, logoPath);

            // Simpan QR Code ke dalam MemoryStream sebagai PNG
            using var qrCodeStream = new MemoryStream();
            qrCodeImage.Save(qrCodeStream, System.Drawing.Imaging.ImageFormat.Png);
            qrCodeStream.Position = 0;

            // Load laporan ke FastReport
            WebReport web = new WebReport();
            var path = $"{_webHostEnvironment.WebRootPath}\\Reporting\\UnitRequest.frx";
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
            web.Report.SetParameterValue("UnitRequestId", Id.ToString());
            web.Report.SetParameterValue("ReqNumber", ReqNumber);
            web.Report.SetParameterValue("CreateDate", CreateDate);
            web.Report.SetParameterValue("CreateBy", CreateBy);
            web.Report.SetParameterValue("UserApprove1", UserApprove1);
            web.Report.SetParameterValue("UnitLocation", UnitLocation);
            web.Report.SetParameterValue("WarehouseLocation", WarehouseLocation);
            web.Report.SetParameterValue("Note", Note);
            web.Report.SetParameterValue("QtyTotal", QtyTotal);

            Stream stream = new MemoryStream();

            web.Report.Prepare();
            web.Report.Export(new PDFSimpleExport(), stream);
            stream.Position = 0;

            return File(stream, "application/pdf");
        }

        [Authorize(Roles = "DownloadUnitRequest")]
        public async Task<IActionResult> DownloadUnitRequest(Guid Id)
        {
            var unitRequest = await _unitRequestRepository.GetUnitRequestById(Id);

            var CreateDate = unitRequest.CreateDateTime.ToString("dd MMMM yyyy");
            var ReqNumber = unitRequest.UnitRequestNumber;
            var CreateBy = unitRequest.ApplicationUser.NamaUser;
            var UserApprove1 = unitRequest.UserApprove1.FullName;
            var UnitLocation = unitRequest.UnitLocation.UnitLocationName;
            var WarehouseLocation = unitRequest.WarehouseLocation.WarehouseLocationName;
            var Note = unitRequest.Note;
            var QtyTotal = unitRequest.QtyTotal;

            // Path logo untuk QR code
            var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.png");

            // Generate QR Code dengan logo
            var qrCodeImage = GenerateQRCodeWithLogo(ReqNumber, logoPath);

            // Simpan QR Code ke dalam MemoryStream sebagai PNG
            using var qrCodeStream = new MemoryStream();
            qrCodeImage.Save(qrCodeStream, System.Drawing.Imaging.ImageFormat.Png);
            qrCodeStream.Position = 0;

            // Load laporan ke FastReport
            WebReport web = new WebReport();
            var path = $"{_webHostEnvironment.WebRootPath}\\Reporting\\UnitRequest.frx";
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
            web.Report.SetParameterValue("UnitRequestId", Id.ToString());
            web.Report.SetParameterValue("ReqNumber", ReqNumber);
            web.Report.SetParameterValue("CreateDate", CreateDate);
            web.Report.SetParameterValue("CreateBy", CreateBy);
            web.Report.SetParameterValue("UserApprove1", UserApprove1);
            web.Report.SetParameterValue("UnitLocation", UnitLocation);
            web.Report.SetParameterValue("WarehouseLocation", WarehouseLocation);
            web.Report.SetParameterValue("Note", Note);
            web.Report.SetParameterValue("QtyTotal", QtyTotal);

            web.Report.Prepare();

            Stream stream = new MemoryStream();
            web.Report.Export(new PDFSimpleExport(), stream);
            stream.Position = 0;

            return File(stream, "application/zip", (ReqNumber + ".pdf"));
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
