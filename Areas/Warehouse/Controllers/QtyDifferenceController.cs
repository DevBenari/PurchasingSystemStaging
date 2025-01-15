using FastReport.Data;
using FastReport.Export.PdfSimple;
using FastReport.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PurchasingSystem.Areas.MasterData.Repositories;
using PurchasingSystem.Areas.Order.Models;
using PurchasingSystem.Areas.Order.Repositories;
using PurchasingSystem.Areas.Transaction.Models;
using PurchasingSystem.Areas.Warehouse.Models;
using PurchasingSystem.Areas.Warehouse.Repositories;
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

namespace PurchasingSystem.Areas.Warehouse.Controllers
{
    [Area("Warehouse")]
    [Route("Warehouse/[Controller]/[Action]")]
    public class QtyDifferenceController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IQtyDifferenceRepository _qtyDifferenceRepository;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IProductRepository _productRepository;
        private readonly ITermOfPaymentRepository _termOfPaymentRepository;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IPurchaseRequestRepository _purchaseRequestRepository;
        private readonly IApprovalQtyDifferenceRepository _approvalQtyDifferenceRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IPositionRepository _positionRepository;
        private readonly IHubContext<ChatHub> _hubContext;

        private readonly IDataProtector _protector;
        private readonly UrlMappingService _urlMappingService;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;


        public QtyDifferenceController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IQtyDifferenceRepository QtyDifferenceRepository,
            IUserActiveRepository userActiveRepository,
            IProductRepository productRepository,
            ITermOfPaymentRepository termOfPaymentRepository,
            IPurchaseOrderRepository purchaseOrderRepository,
            IPurchaseRequestRepository purchaseRequestRepository,
            IApprovalQtyDifferenceRepository approvalQtyDifferenceRepository,
            IDepartmentRepository departmentRepository,
            IPositionRepository positionRepository,
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
            _qtyDifferenceRepository = QtyDifferenceRepository;
            _userActiveRepository = userActiveRepository;
            _productRepository = productRepository;
            _termOfPaymentRepository = termOfPaymentRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
            _purchaseRequestRepository = purchaseRequestRepository;
            _approvalQtyDifferenceRepository = approvalQtyDifferenceRepository;
            _departmentRepository = departmentRepository;
            _positionRepository = positionRepository;
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

        public JsonResult LoadPurchaseOrder(Guid Id)
        {
            var podetail = _applicationDbContext.PurchaseOrders
                .Include(p => p.PurchaseOrderDetails)
                .Where(p => p.PurchaseOrderId == Id).FirstOrDefault();
            return new JsonResult(podetail);
        }

        public JsonResult LoadPurchaseOrderDetail(Guid Id)
        {
            var podetail = _applicationDbContext.PurchaseOrderDetails
                .Where(p => p.PurchaseOrderDetailId == Id).FirstOrDefault();
            return new JsonResult(podetail);
        }
        
        [HttpGet]
        [Authorize(Roles = "ReadQtyDifference")]
        public async Task<IActionResult> Index(string filterOptions = "", string searchTerm = "", DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, int page = 1, int pageSize = 10)
        {
            ViewBag.Active = "QtyDifference";
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

            var data = await _qtyDifferenceRepository.GetAllQtyDifferencePageSize(searchTerm, page, pageSize, startDate, endDate);

            var model = new Pagination<QtyDifference>
            {
                Items = data.qtyDifferences.Where(u => u.CreateBy.ToString() == getUserLogin.Id).ToList(),
                TotalCount = data.totalCountQtyDifferences,
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
        [Authorize(Roles = "CreateQtyDifference")]
        public async Task<IActionResult> CreateQtyDifference(string poList)
        {
            ViewBag.Active = "QtyDifference";

            //Pembelian Pembelian = await _pembelianRepository.GetAllPembelian().Where(p => p.PembelianNumber == );

            _signInManager.IsSignedIn(User);
            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            
            ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
            ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
            ViewBag.POFilter = new SelectList(await _purchaseOrderRepository.GetPurchaseOrdersFilters(), "PurchaseOrderId", "PurchaseOrderNumber", SortOrder.Ascending);
            ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
            ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);

            QtyDifference QtyDifference = new QtyDifference()
            {
                UserAccessId = getUser.Id,
            }; ;            

            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _qtyDifferenceRepository.GetAllQtyDifference().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.QtyDifferenceNumber).FirstOrDefault();
            if (lastCode == null)
            {
                QtyDifference.QtyDifferenceNumber = "QD" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.QtyDifferenceNumber.Substring(2, 6);

                if (lastCodeTrim != setDateNow)
                {
                    QtyDifference.QtyDifferenceNumber = "QD" + setDateNow + "0001";
                }
                else
                {
                    QtyDifference.QtyDifferenceNumber = "QD" + setDateNow + (Convert.ToInt32(lastCode.QtyDifferenceNumber.Substring(9, lastCode.QtyDifferenceNumber.Length - 9)) + 1).ToString("D4");
                }
            }

            return View(QtyDifference);
        }


        [HttpPost]
        [Authorize(Roles = "CreateQtyDifference")]
        public async Task<IActionResult> CreateQtyDifference(QtyDifference model)
        {
            ViewBag.Active = "QtyDifference";            

            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _qtyDifferenceRepository.GetAllQtyDifference().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.QtyDifferenceNumber).FirstOrDefault();
            if (lastCode == null)
            {
                model.QtyDifferenceNumber = "QD" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.QtyDifferenceNumber.Substring(2, 6);

                if (lastCodeTrim != setDateNow)
                {
                    model.QtyDifferenceNumber = "QD" + setDateNow + "0001";
                }
                else
                {
                    model.QtyDifferenceNumber = "QD" + setDateNow + (Convert.ToInt32(lastCode.QtyDifferenceNumber.Substring(9, lastCode.QtyDifferenceNumber.Length - 9)) + 1).ToString("D4");
                }
            }

            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            if (ModelState.IsValid)
            {
                var QtyDifference = new QtyDifference
                {
                    CreateDateTime = DateTime.Now,
                    CreateBy = new Guid(getUser.Id), //Convert Guid to String
                    QtyDifferenceId = model.QtyDifferenceId,
                    QtyDifferenceNumber = model.QtyDifferenceNumber,
                    PurchaseOrderId = model.PurchaseOrderId,
                    PurchaseOrderNumber = model.PurchaseOrderNumber,
                    UserAccessId = getUser.Id,
                    Department1Id = model.Department1Id,
                    Position1Id = model.Position1Id,
                    UserApprove1Id = model.UserApprove1Id,
                    ApproveStatusUser1 = model.ApproveStatusUser1,
                    Department2Id = model.Department2Id,
                    Position2Id = model.Position2Id,
                    UserApprove2Id = model.UserApprove2Id,
                    ApproveStatusUser2 = model.ApproveStatusUser2,
                    Status = model.Status,
                    Note = model.Note,
                    MessageApprove1 = model.MessageApprove1,
                    MessageApprove2 = model.MessageApprove2,
                    QtyDifferenceDetails = model.QtyDifferenceDetails,
                };
               
                _qtyDifferenceRepository.Tambah(QtyDifference);

                var updateStatusPO = _purchaseOrderRepository.GetAllPurchaseOrder().Where(c => c.PurchaseOrderId == model.PurchaseOrderId).FirstOrDefault();
                if (updateStatusPO != null)
                {
                    updateStatusPO.UpdateDateTime = DateTime.Now;
                    updateStatusPO.UpdateBy = new Guid(getUser.Id);
                    updateStatusPO.Status = "Process Cancel";

                    _applicationDbContext.Entry(updateStatusPO).State = EntityState.Modified;
                }

                if (model.UserApprove1Id != null)
                {
                    var approvalQtyDiffRequest = new ApprovalQtyDifference
                    {
                        CreateDateTime = DateTime.Now,
                        CreateBy = new Guid(getUser.Id), //Convert Guid to String
                        QtyDifferenceId = QtyDifference.QtyDifferenceId,
                        QtyDifferenceNumber = QtyDifference.QtyDifferenceNumber,
                        UserAccessId = getUser.Id.ToString(),
                        PurchaseOrderId = QtyDifference.PurchaseOrderId,
                        PurchaseOrderNumber = QtyDifference.PurchaseOrderNumber,
                        UserApproveId = QtyDifference.UserApprove1Id,
                        ApproveBy = "",
                        ApprovalTime = "",
                        ApprovalDate = DateTimeOffset.MinValue,
                        ApprovalStatusUser = "User1",
                        Status = QtyDifference.Status,
                        Note = QtyDifference.Note,
                        Message = QtyDifference.MessageApprove1
                    };
                    _approvalQtyDifferenceRepository.Tambah(approvalQtyDiffRequest);
                }

                if (model.UserApprove2Id != null)
                {
                    var approvalQtyDiffRequest = new ApprovalQtyDifference
                    {
                        CreateDateTime = DateTime.Now,
                        CreateBy = new Guid(getUser.Id), //Convert Guid to String
                        QtyDifferenceId = QtyDifference.QtyDifferenceId,
                        QtyDifferenceNumber = QtyDifference.QtyDifferenceNumber,
                        UserAccessId = getUser.Id.ToString(),
                        PurchaseOrderId = QtyDifference.PurchaseOrderId,
                        PurchaseOrderNumber = QtyDifference.PurchaseOrderNumber,
                        UserApproveId = QtyDifference.UserApprove1Id,
                        ApproveBy = "",
                        ApprovalTime = "",
                        ApprovalDate = DateTimeOffset.MinValue,
                        ApprovalStatusUser = "User2",
                        Status = QtyDifference.Status,
                        Note = QtyDifference.Note,
                        Message = QtyDifference.MessageApprove1
                    };
                    _approvalQtyDifferenceRepository.Tambah(approvalQtyDiffRequest);
                }

                TempData["SuccessMessage"] = "Number " + model.QtyDifferenceNumber + " Saved";
                return Json(new { redirectToUrl = Url.Action("Index", "QtyDifference") });
            }
            else
            {
                ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
                ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
                ViewBag.POFilter = new SelectList(await _purchaseOrderRepository.GetPurchaseOrdersFilters(), "PurchaseOrderId", "PurchaseOrderNumber", SortOrder.Ascending);
                ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
                ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
                TempData["WarningMessage"] = "There is still empty data!!!";
                return View(model);
            }
        }
        
        [HttpGet]
        [Authorize(Roles = "UpdateQtyDifference")]
        public async Task<IActionResult> DetailQtyDifference(Guid Id)
        {
            ViewBag.Active = "QtyDifference";

            ViewBag.PO = new SelectList(await _purchaseOrderRepository.GetPurchaseOrders(), "PurchaseOrderId", "PurchaseOrderNumber", SortOrder.Ascending);
            ViewBag.User = new SelectList(_userManager.Users, nameof(ApplicationUser.Id), nameof(ApplicationUser.NamaUser), SortOrder.Ascending);
            ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
            ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
            ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);

            var QtyDifference = await _qtyDifferenceRepository.GetQtyDifferenceById(Id);

            if (QtyDifference == null)
            {
                Response.StatusCode = 404;
                return View("QtyDifferenceNotFound", Id);
            }

            QtyDifference model = new QtyDifference
            {
                QtyDifferenceId = QtyDifference.QtyDifferenceId,
                QtyDifferenceNumber = QtyDifference.QtyDifferenceNumber,
                PurchaseOrderId = QtyDifference.PurchaseOrderId,
                PurchaseOrderNumber = QtyDifference.PurchaseOrderNumber,
                UserAccessId = QtyDifference.UserAccessId,
                Department1Id = QtyDifference.Department1Id,
                Position1Id = QtyDifference.Position1Id,
                UserApprove1Id = QtyDifference.UserApprove1Id,
                ApproveStatusUser1 = QtyDifference.ApproveStatusUser1,
                Department2Id = QtyDifference.Department2Id,
                Position2Id = QtyDifference.Position2Id,
                UserApprove2Id = QtyDifference.UserApprove2Id,
                ApproveStatusUser2 = QtyDifference.ApproveStatusUser2,
                Status = QtyDifference.Status,
                Note = QtyDifference.Note,
                MessageApprove1 = QtyDifference.MessageApprove1,
                MessageApprove2 = QtyDifference.MessageApprove2,
                QtyDifferenceDetails = QtyDifference.QtyDifferenceDetails,
            };
            return View(model);
        }

        [Authorize(Roles = "PreviewQtyDifference")]
        public async Task<IActionResult> PreviewQtyDifference(Guid Id)
        {
            var qtyDifference = await _qtyDifferenceRepository.GetQtyDifferenceById(Id);

            var CreateDate = qtyDifference.CreateDateTime.ToString("dd MMMM yyyy");
            var QdNumber = qtyDifference.QtyDifferenceNumber;
            var PoNumber = qtyDifference.PurchaseOrder.PurchaseOrderNumber;
            var UserApprove1 = qtyDifference.UserApprove1.FullName;
            var UserApprove2 = qtyDifference.UserApprove2.FullName;
            var Note = qtyDifference.Note;

            // Path logo untuk QR code
            var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.png");

            // Generate QR Code dengan logo
            var qrCodeImage = GenerateQRCodeWithLogo(QdNumber, logoPath);

            // Simpan QR Code ke dalam MemoryStream sebagai PNG
            using var qrCodeStream = new MemoryStream();
            qrCodeImage.Save(qrCodeStream, System.Drawing.Imaging.ImageFormat.Png);
            qrCodeStream.Position = 0;

            // Load laporan ke FastReport
            WebReport web = new WebReport();
            var path = $"{_webHostEnvironment.WebRootPath}\\Reporting\\CancelPurchaseOrder.frx";
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
            web.Report.SetParameterValue("QtyDifferenceId", Id.ToString());
            web.Report.SetParameterValue("QdNumber", QdNumber);
            web.Report.SetParameterValue("PoNumber", PoNumber);
            web.Report.SetParameterValue("CreateDate", CreateDate);
            web.Report.SetParameterValue("UserApprove1", UserApprove1);
            web.Report.SetParameterValue("UserApprove2", UserApprove2);
            web.Report.SetParameterValue("Note", Note);

            Stream stream = new MemoryStream();

            web.Report.Prepare();
            web.Report.Export(new PDFSimpleExport(), stream);
            stream.Position = 0;

            return File(stream, "application/pdf");
        }

        [Authorize(Roles = "DownloadQtyDifference")]
        public async Task<IActionResult> DownloadQtyDifference(Guid Id)
        {
            var qtyDifference = await _qtyDifferenceRepository.GetQtyDifferenceById(Id);

            var CreateDate = qtyDifference.CreateDateTime.ToString("dd MMMM yyyy");
            var QdNumber = qtyDifference.QtyDifferenceNumber;
            var PoNumber = qtyDifference.PurchaseOrder.PurchaseOrderNumber;
            var UserApprove1 = qtyDifference.UserApprove1.FullName;
            var UserApprove2 = qtyDifference.UserApprove2.FullName;
            var Note = qtyDifference.Note;

            // Path logo untuk QR code
            var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.png");

            // Generate QR Code dengan logo
            var qrCodeImage = GenerateQRCodeWithLogo(QdNumber, logoPath);

            // Simpan QR Code ke dalam MemoryStream sebagai PNG
            using var qrCodeStream = new MemoryStream();
            qrCodeImage.Save(qrCodeStream, System.Drawing.Imaging.ImageFormat.Png);
            qrCodeStream.Position = 0;

            // Load laporan ke FastReport
            WebReport web = new WebReport();
            var path = $"{_webHostEnvironment.WebRootPath}\\Reporting\\CancelPurchaseOrder.frx";
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
            web.Report.SetParameterValue("QtyDifferenceId", Id.ToString());
            web.Report.SetParameterValue("QdNumber", QdNumber);
            web.Report.SetParameterValue("PoNumber", PoNumber);
            web.Report.SetParameterValue("CreateDate", CreateDate);
            web.Report.SetParameterValue("UserApprove1", UserApprove1);
            web.Report.SetParameterValue("UserApprove2", UserApprove2);
            web.Report.SetParameterValue("Note", Note);

            web.Report.Prepare();

            Stream stream = new MemoryStream();
            web.Report.Export(new PDFSimpleExport(), stream);
            stream.Position = 0;

            return File(stream, "application/zip", (QdNumber + ".pdf"));
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
