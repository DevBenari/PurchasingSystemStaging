using FastReport.Data;
using FastReport.Export.PdfSimple;
using FastReport.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PurchasingSystem.Areas.Order.Repositories;
using PurchasingSystem.Areas.Report.Models;
using PurchasingSystem.Areas.Report.Repositories;
using PurchasingSystem.Areas.Warehouse.Models;
using PurchasingSystem.Areas.Warehouse.Repositories;
using PurchasingSystem.Data;
using PurchasingSystem.Models;
using PurchasingSystem.Repositories;
using QRCoder;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PurchasingSystem.Areas.Report.Controllers
{
    [Area("Report")]
    [Route("Report/[Controller]/[Action]")]
    public class ClosedPurchaseOrderController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IClosingPurchaseOrderRepository _closingPurchaseOrderRepository;

        private readonly IDataProtector _protector;
        private readonly UrlMappingService _urlMappingService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        public ClosedPurchaseOrderController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IClosingPurchaseOrderRepository closingPurchaseOrderRepository,            

            IDataProtectionProvider provider,
            UrlMappingService urlMappingService,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _closingPurchaseOrderRepository = closingPurchaseOrderRepository;

            _protector = provider.CreateProtector("UrlProtector");
            _urlMappingService = urlMappingService;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
        }
       
        [HttpGet]
        [Authorize(Roles = "ReadClosedPurchaseOrder")]
        public async Task<IActionResult> Index(string filterOptions = "", string searchTerm = "", DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, int page = 1, int pageSize = 10)
        {
            ViewBag.Active = "Report";
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

            //var getUserLogin = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            //var getUserActive = _userActiveRepository.GetAllUser().Where(c => c.UserActiveCode == getUserLogin.KodeUser).FirstOrDefault();

            var data = await _closingPurchaseOrderRepository.GetAllClosingPurchaseOrderPageSize(searchTerm, page, pageSize, startDate, endDate);

            var model = new Pagination<ClosingPurchaseOrder>
            {
                Items = data.ClosingPurchaseOrders,
                TotalCount = data.totalCountClosingPurchaseOrders,
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
        public async Task<IActionResult> DetailClosedPurchaseOrder(Guid Id)
        {
            ViewBag.Active = "Report";
            ViewBag.User = new SelectList(_userManager.Users, nameof(ApplicationUser.Id), nameof(ApplicationUser.NamaUser), SortOrder.Ascending);

            var closedPO = await _closingPurchaseOrderRepository.GetClosingPurchaseOrderById(Id);

            if (closedPO == null)
            {
                Response.StatusCode = 404;
                return View("ClosedPurchaseOrderNotFound", Id);
            }

            var model = new ClosingPurchaseOrder
            {
                ClosingPurchaseOrderId = closedPO.ClosingPurchaseOrderId,
                ClosingPurchaseOrderNumber = closedPO.ClosingPurchaseOrderNumber,
                UserAccessId = closedPO.UserAccessId,
                Month = closedPO.Month,
                Year = closedPO.Year,
                TotalPo = closedPO.TotalPo,
                TotalQty = closedPO.TotalQty,
                GrandTotal = closedPO.GrandTotal,
                ClosingPurchaseOrderDetails = closedPO.ClosingPurchaseOrderDetails,
            };
            return View(model);
        }

        [Authorize(Roles = "PreviewClosingPurchaseOrder")]
        public async Task<IActionResult> PreviewClosingPurchaseOrder(Guid Id)
        {
            var ClosingPurchaseOrder = await _closingPurchaseOrderRepository.GetClosingPurchaseOrderById(Id);

            var CreateDate = ClosingPurchaseOrder.CreateDateTime.ToString("dd MMMM yyyy");
            var ClosingPurchaseOrderNumber = ClosingPurchaseOrder.ClosingPurchaseOrderNumber;
            var CreateBy = ClosingPurchaseOrder.ApplicationUser.NamaUser;
            var MonthNumber = ClosingPurchaseOrder.Month;
            var Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(MonthNumber);
            var Year = ClosingPurchaseOrder.Year;
            var TotalPO = ClosingPurchaseOrder.TotalPo;
            var TotalQty = ClosingPurchaseOrder.TotalQty;
            var GrandTotal = ClosingPurchaseOrder.GrandTotal;           

            // Path logo untuk QR code
            var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.png");

            // Generate QR Code dengan logo
            var qrCodeImage = GenerateQRCodeWithLogo(ClosingPurchaseOrderNumber, logoPath);

            // Simpan QR Code ke dalam MemoryStream sebagai PNG
            using var qrCodeStream = new MemoryStream();
            qrCodeImage.Save(qrCodeStream, System.Drawing.Imaging.ImageFormat.Png);
            qrCodeStream.Position = 0;

            WebReport web = new WebReport();
            var path = $"{_webHostEnvironment.WebRootPath}\\Reporting\\ClosingPurchaseOrder.frx";
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
            web.Report.SetParameterValue("ClosingPurchaseOrderId", Id.ToString());
            web.Report.SetParameterValue("ClosingPurchaseOrderNumber", ClosingPurchaseOrderNumber);
            web.Report.SetParameterValue("CreateDate", CreateDate);
            web.Report.SetParameterValue("CreateBy", CreateBy);
            web.Report.SetParameterValue("Month", Month);
            web.Report.SetParameterValue("Year", Year);
            web.Report.SetParameterValue("TotalPO", TotalPO);
            web.Report.SetParameterValue("TotalQty", TotalQty);
            web.Report.SetParameterValue("GrandTotal", GrandTotal);

            Stream stream = new MemoryStream();

            web.Report.Prepare();
            web.Report.Export(new PDFSimpleExport(), stream);
            stream.Position = 0;

            return File(stream, "application/pdf");
        }

        [Authorize(Roles = "DownloadClosingPurchaseOrder")]
        public async Task<IActionResult> DownloadClosingPurchaseOrder(Guid Id)
        {
            var ClosingPurchaseOrder = await _closingPurchaseOrderRepository.GetClosingPurchaseOrderById(Id);

            var CreateDate = ClosingPurchaseOrder.CreateDateTime.ToString("dd MMMM yyyy");
            var PrNumber = ClosingPurchaseOrder.ClosingPurchaseOrderNumber;
            var CreateBy = ClosingPurchaseOrder.ApplicationUser.NamaUser;
            var MonthNumber = ClosingPurchaseOrder.Month;
            var Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(MonthNumber);
            var Year = ClosingPurchaseOrder.Year;
            var TotalPO = ClosingPurchaseOrder.TotalPo;
            var TotalQty = ClosingPurchaseOrder.TotalQty;
            var GrandTotal = ClosingPurchaseOrder.GrandTotal;

            // Path logo untuk QR code
            var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.png");

            // Generate QR Code dengan logo
            var qrCodeImage = GenerateQRCodeWithLogo(PrNumber, logoPath);

            // Simpan QR Code ke dalam MemoryStream sebagai PNG
            using var qrCodeStream = new MemoryStream();
            qrCodeImage.Save(qrCodeStream, System.Drawing.Imaging.ImageFormat.Png);
            qrCodeStream.Position = 0;

            WebReport web = new WebReport();
            var path = $"{_webHostEnvironment.WebRootPath}\\Reporting\\ClosingPurchaseOrder.frx";
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
            web.Report.SetParameterValue("ClosingPurchaseOrderId", Id.ToString());
            web.Report.SetParameterValue("PrNumber", PrNumber);
            web.Report.SetParameterValue("CreateDate", CreateDate);
            web.Report.SetParameterValue("CreateBy", CreateBy);
            web.Report.SetParameterValue("Month", Month);
            web.Report.SetParameterValue("Year", Year);
            web.Report.SetParameterValue("TotalPO", TotalPO);
            web.Report.SetParameterValue("TotalQty", TotalQty);
            web.Report.SetParameterValue("GrandTotal", GrandTotal);

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
