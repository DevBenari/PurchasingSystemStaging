﻿using FastReport.Data;
using FastReport.Export.PdfSimple;
using FastReport.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PurchasingSystem.Areas.MasterData.Repositories;
using PurchasingSystem.Areas.Order.Models;
using PurchasingSystem.Areas.Order.Repositories;
using PurchasingSystem.Areas.Order.ViewModels;
using PurchasingSystem.Areas.Warehouse.Repositories;
using PurchasingSystem.Data;
using PurchasingSystem.Models;
using PurchasingSystem.Repositories;
using QRCoder;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace PurchasingSystem.Areas.Order.Controllers
{
    [Area("Order")]
    [Route("Order/[Controller]/[Action]")]
    public class PurchaseOrderController : Controller
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
        private readonly IQtyDifferenceRepository _qtyDifferenceRepository;

        private readonly IDataProtector _protector;
        private readonly UrlMappingService _urlMappingService;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;


        public PurchaseOrderController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IPurchaseRequestRepository purchaseRequestRepository,
            IUserActiveRepository userActiveRepository,
            IProductRepository productRepository,
            ITermOfPaymentRepository termOfPaymentRepository,
            IApprovalPurchaseRequestRepository approvalRepository,
            IPurchaseOrderRepository purchaseOrderRepository,
            IQtyDifferenceRepository qtyDifferenceRepository,

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
            _qtyDifferenceRepository = qtyDifferenceRepository;

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
            var qtyDiff = _applicationDbContext.QtyDifferences.Where(q => q.PurchaseOrderId == podetail.PurchaseOrderId).FirstOrDefault();
            var qtyDiffDetail = _applicationDbContext.QtyDifferenceDetails
                .Where(d => d.QtyDifferenceId == qtyDiff.QtyDifferenceId && d.ProductNumber == podetail.ProductNumber).FirstOrDefault();
            return new JsonResult(qtyDiffDetail);
        }        

        [HttpGet]
        [Authorize(Roles = "ReadPurchaseOrder")]
        public async Task<IActionResult> Index(string filterOptions = "", string searchTerm = "", DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, int page = 1, int pageSize = 10)
        {
            ViewBag.Active = "PurchaseOrder";
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

            var data = await _purchaseOrderRepository.GetAllPurchaseOrderPageSize(searchTerm, page, pageSize, startDate, endDate);

            var model = new Pagination<PurchaseOrder>
            {
                Items = data.purchaseOrders,
                TotalCount = data.totalCountPurchaseOrders,
                PageSize = pageSize,
                CurrentPage = page,
            };

            return View(model);
        }
        
        [HttpGet]
        [Authorize(Roles = "UpdatePurchaseOrder")]
        public async Task<IActionResult> DetailPurchaseOrder(Guid Id)
        {
            ViewBag.Active = "PurchaseOrder";

            ViewBag.User = new SelectList(_userManager.Users, nameof(ApplicationUser.Id), nameof(ApplicationUser.NamaUser), SortOrder.Ascending);
            ViewBag.Pr = new SelectList(await _purchaseRequestRepository.GetPurchaseRequests(), "PurchaseRequestId", "PurchaseRequestNumber", SortOrder.Ascending);
            ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
            ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
            ViewBag.TermOfPayment = new SelectList(await _termOfPaymentRepository.GetTermOfPayments(), "TermOfPaymentId", "TermOfPaymentName", SortOrder.Ascending);

            var purchaseOrder = await _purchaseOrderRepository.GetPurchaseOrderById(Id);

            if (purchaseOrder == null)
            {
                Response.StatusCode = 404;
                return View("PurchaseOrderNotFound", Id);
            }

            PurchaseOrder model = new PurchaseOrder
            {
                PurchaseOrderId = purchaseOrder.PurchaseOrderId,
                PurchaseOrderNumber = purchaseOrder.PurchaseOrderNumber,
                PurchaseRequestId = purchaseOrder.PurchaseRequestId,
                PurchaseRequestNumber = purchaseOrder.PurchaseRequestNumber,
                UserAccessId = purchaseOrder.UserAccessId,
                UserApprove1Id = purchaseOrder.UserApprove1Id,
                UserApprove2Id = purchaseOrder.UserApprove2Id,
                UserApprove3Id = purchaseOrder.UserApprove3Id,
                TermOfPaymentId = purchaseOrder.TermOfPaymentId,
                Status = purchaseOrder.Status,
                QtyTotal = purchaseOrder.QtyTotal,
                GrandTotal = Math.Truncate(purchaseOrder.GrandTotal),
                Note = purchaseOrder.Note
            };

            var ItemsList = new List<PurchaseOrderDetail>();

            var culture = new CultureInfo("id-ID");

            foreach (var item in purchaseOrder.PurchaseOrderDetails)
            {
                ItemsList.Add(new PurchaseOrderDetail
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

            model.PurchaseOrderDetails = ItemsList;

            return View(model);
        }

        [Authorize(Roles = "PreviewPurchaseOrder")]
        public async Task<IActionResult> PreviewPurchaseOrder(Guid Id)
        {
            var purchaseOrder = await _purchaseOrderRepository.GetPurchaseOrderById(Id);

            var CreateDate = purchaseOrder.CreateDateTime.ToString("dd MMMM yyyy");
            var PoNumber = purchaseOrder.PurchaseOrderNumber;
            var CreateBy = purchaseOrder.ApplicationUser.NamaUser;
            var UserApprove1 = purchaseOrder.UserApprove1.FullName;
            var UserApprove2 = purchaseOrder.UserApprove2.FullName;
            var UserApprove3 = purchaseOrder.UserApprove3.FullName;
            var TermOfPayment = purchaseOrder.TermOfPayment.TermOfPaymentName;
            var Note = purchaseOrder.Note;
            var GrandTotal = purchaseOrder.GrandTotal;
            var Tax = (GrandTotal / 100) * 11;
            var GrandTotalAfterTax = (GrandTotal + Tax);

            // Path logo untuk QR code
            var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.png");

            // Generate QR Code dengan logo
            var qrCodeImage = GenerateQRCodeWithLogo(PoNumber, logoPath);

            // Simpan QR Code ke dalam MemoryStream sebagai PNG
            using var qrCodeStream = new MemoryStream();
            qrCodeImage.Save(qrCodeStream, System.Drawing.Imaging.ImageFormat.Png);
            qrCodeStream.Position = 0;

            WebReport web = new WebReport();
            var path = $"{_webHostEnvironment.WebRootPath}\\Reporting\\PurchaseOrder.frx";
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
            web.Report.SetParameterValue("PurchaseOrderId", Id.ToString());
            web.Report.SetParameterValue("PoNumber", PoNumber);
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

        [Authorize(Roles = "DownloadPurchaseOrder")]
        public async Task<IActionResult> DownloadPurchaseOrder(Guid Id)
        {
            var purchaseOrder = await _purchaseOrderRepository.GetPurchaseOrderById(Id);

            var CreateDate = purchaseOrder.CreateDateTime.ToString("dd MMMM yyyy");
            var PoNumber = purchaseOrder.PurchaseOrderNumber;
            var CreateBy = purchaseOrder.ApplicationUser.NamaUser;
            var UserApprove1 = purchaseOrder.UserApprove1.FullName;
            var UserApprove2 = purchaseOrder.UserApprove2.FullName;
            var UserApprove3 = purchaseOrder.UserApprove3.FullName;
            var TermOfPayment = purchaseOrder.TermOfPayment.TermOfPaymentName;
            var Note = purchaseOrder.Note;
            var GrandTotal = purchaseOrder.GrandTotal;
            var Tax = (GrandTotal / 100) * 11;
            var GrandTotalAfterTax = (GrandTotal + Tax);

            // Path logo untuk QR code
            var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.png");

            // Generate QR Code dengan logo
            var qrCodeImage = GenerateQRCodeWithLogo(PoNumber, logoPath);

            // Simpan QR Code ke dalam MemoryStream sebagai PNG
            using var qrCodeStream = new MemoryStream();
            qrCodeImage.Save(qrCodeStream, System.Drawing.Imaging.ImageFormat.Png);
            qrCodeStream.Position = 0;

            WebReport web = new WebReport();
            var path = $"{_webHostEnvironment.WebRootPath}\\Reporting\\PurchaseOrder.frx";
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
            web.Report.SetParameterValue("PurchaseOrderId", Id.ToString());
            web.Report.SetParameterValue("PoNumber", PoNumber);
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

            return File(stream, "application/zip", (PoNumber + ".pdf"));
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
