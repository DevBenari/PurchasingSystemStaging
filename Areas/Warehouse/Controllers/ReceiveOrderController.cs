using FastReport.Data;
using FastReport.Export.Html;
using FastReport.Export.PdfSimple;
using FastReport.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PurchasingSystem.Areas.MasterData.Repositories;
using PurchasingSystem.Areas.Order.Models;
using PurchasingSystem.Areas.Order.Repositories;
using PurchasingSystem.Areas.Warehouse.Models;
using PurchasingSystem.Areas.Warehouse.Repositories;
using PurchasingSystem.Data;
using PurchasingSystem.Models;
using PurchasingSystem.Repositories;
using QRCoder;
using System.Data;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;

namespace PurchasingSystem.Areas.Warehouse.Controllers
{
    [Area("Warehouse")]
    [Route("Warehouse/[Controller]/[Action]")]
    public class ReceiveOrderController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IReceiveOrderRepository _receiveOrderRepository;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IProductRepository _productRepository;
        private readonly ITermOfPaymentRepository _termOfPaymentRepository;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IPurchaseRequestRepository _purchaseRequestRepository;

        private readonly IDataProtector _protector;
        private readonly UrlMappingService _urlMappingService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        public ReceiveOrderController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IReceiveOrderRepository receiveOrderRepository,
            IUserActiveRepository userActiveRepository,
            IProductRepository productRepository,
            ITermOfPaymentRepository termOfPaymentRepository,
            IPurchaseOrderRepository purchaseOrderRepository,
            IPurchaseRequestRepository purchaseRequestRepository,

            IDataProtectionProvider provider,
            UrlMappingService urlMappingService,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration

        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _receiveOrderRepository = receiveOrderRepository;
            _userActiveRepository = userActiveRepository;
            _productRepository = productRepository;
            _termOfPaymentRepository = termOfPaymentRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
            _purchaseRequestRepository = purchaseRequestRepository;

            _protector = provider.CreateProtector("UrlProtector");
            _urlMappingService = urlMappingService;
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
            return new JsonResult(podetail);
        }
       
        [HttpGet]
        [Authorize(Roles = "ReadReceiveOrder")]
        public async Task<IActionResult> Index(string filterOptions = "", string searchTerm = "", DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, int page = 1, int pageSize = 10)
        {
            ViewBag.Active = "ReceiveOrder";
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

            var data = await _receiveOrderRepository.GetAllReceiveOrderPageSize(searchTerm, page, pageSize, startDate, endDate);

            var model = new Pagination<ReceiveOrder>
            {
                Items = data.receiveOrders,
                TotalCount = data.totalCountReceiveOrders,
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
        [Authorize(Roles = "CreateReceiveOrder")]
        public async Task<IActionResult> CreateReceiveOrder(string poList)
        {
            ViewBag.Active = "ReceiveOrder";
            //Pembelian Pembelian = await _pembelianRepository.GetAllPembelian().Where(p => p.PembelianNumber == );

            _signInManager.IsSignedIn(User);
            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
            ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
            ViewBag.POFilter = new SelectList(await _purchaseOrderRepository.GetPurchaseOrdersFilters(), "PurchaseOrderId", "PurchaseOrderNumber", SortOrder.Ascending);

            ReceiveOrder ReceiveOrder = new ReceiveOrder()
            {
                ReceiveById = getUser.Id,
            };
            //ReceiveOrder.ReceiveOrderDetails.Add(new ReceiveOrderDetail() { ReceiveOrderDetailId = Guid.NewGuid() });

            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _receiveOrderRepository.GetAllReceiveOrder().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.ReceiveOrderNumber).FirstOrDefault();
            if (lastCode == null)
            {
                ReceiveOrder.ReceiveOrderNumber = "RO" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.ReceiveOrderNumber.Substring(2, 6);

                if (lastCodeTrim != setDateNow)
                {
                    ReceiveOrder.ReceiveOrderNumber = "RO" + setDateNow + "0001";
                }
                else
                {
                    ReceiveOrder.ReceiveOrderNumber = "RO" + setDateNow + (Convert.ToInt32(lastCode.ReceiveOrderNumber.Substring(9, lastCode.ReceiveOrderNumber.Length - 9)) + 1).ToString("D4");
                }
            }

            return View(ReceiveOrder);
        }


        [HttpPost]
        [Authorize(Roles = "CreateReceiveOrder")]
        public async Task<IActionResult> CreateReceiveOrder(ReceiveOrder model)
        {
            ViewBag.Active = "ReceiveOrder";
            ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
            ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
            ViewBag.POFilter = new SelectList(await _purchaseOrderRepository.GetPurchaseOrdersFilters(), "PurchaseOrderId", "PurchaseOrderNumber", SortOrder.Ascending);

            var checkDuplicate = _receiveOrderRepository.GetAllReceiveOrder().Where(r => r.PurchaseOrderId == model.PurchaseOrderId).ToList();
            
            if (checkDuplicate.Count == 0) 
            {
                var dateNow = DateTimeOffset.Now;
                var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

                var lastCode = _receiveOrderRepository.GetAllReceiveOrder().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.ReceiveOrderNumber).FirstOrDefault();
                if (lastCode == null)
                {
                    model.ReceiveOrderNumber = "RO" + setDateNow + "0001";
                }
                else
                {
                    var lastCodeTrim = lastCode.ReceiveOrderNumber.Substring(2, 6);

                    if (lastCodeTrim != setDateNow)
                    {
                        model.ReceiveOrderNumber = "RO" + setDateNow + "0001";
                    }
                    else
                    {
                        model.ReceiveOrderNumber = "RO" + setDateNow + (Convert.ToInt32(lastCode.ReceiveOrderNumber.Substring(9, lastCode.ReceiveOrderNumber.Length - 9)) + 1).ToString("D4");
                    }
                }

                var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

                if (ModelState.IsValid)
                {
                    var receiveOrder = new ReceiveOrder
                    {
                        CreateDateTime = DateTime.Now,
                        CreateBy = new Guid(getUser.Id), //Convert Guid to String
                        ReceiveOrderId = model.ReceiveOrderId,
                        ReceiveOrderNumber = model.ReceiveOrderNumber,
                        PurchaseOrderId = model.PurchaseOrderId,
                        ReceiveById = getUser.Id,
                        ShippingNumber = model.ShippingNumber,
                        DeliveryServiceName = model.DeliveryServiceName,
                        DeliveryDate = model.DeliveryDate,
                        WaybillNumber = model.WaybillNumber,
                        InvoiceNumber = model.InvoiceNumber,
                        SenderName = model.SenderName,
                        Status = model.Status,
                        Note = model.Note,
                        ReceiveOrderDetails = model.ReceiveOrderDetails,
                    };

                    foreach (var item in receiveOrder.ReceiveOrderDetails)
                    {
                        var updateProduk = _productRepository.GetAllProduct().Where(c => c.ProductCode == item.ProductNumber).FirstOrDefault();
                        if (updateProduk != null)
                        {
                            updateProduk.UpdateDateTime = DateTime.Now;
                            updateProduk.UpdateBy = new Guid(getUser.Id);
                            updateProduk.Stock = updateProduk.Stock + item.QtyReceive;

                            _applicationDbContext.Entry(updateProduk).State = EntityState.Modified;
                        }
                    }

                    var updateStatusPO = _purchaseOrderRepository.GetAllPurchaseOrder().Where(c => c.PurchaseOrderId == model.PurchaseOrderId).FirstOrDefault();
                    if (updateStatusPO != null)
                    {
                        updateStatusPO.UpdateDateTime = DateTime.Now;
                        updateStatusPO.UpdateBy = new Guid(getUser.Id);
                        updateStatusPO.Status = model.ReceiveOrderNumber;

                        _applicationDbContext.Entry(updateStatusPO).State = EntityState.Modified;
                    }                    

                    _receiveOrderRepository.Tambah(receiveOrder);
                    TempData["SuccessMessage"] = "Number " + model.ReceiveOrderNumber + " Saved";
                    return Json(new { redirectToUrl = Url.Action("Index", "ReceiveOrder") });
                }
                else
                {
                    ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
                    ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
                    ViewBag.POFilter = new SelectList(await _purchaseOrderRepository.GetPurchaseOrdersFilters(), "PurchaseOrderId", "PurchaseOrderNumber", SortOrder.Ascending);
                    TempData["WarningMessage"] = "There is still empty data!!!";
                    return View(model);
                }
            }
            else
            {
                ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);
                ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
                ViewBag.POFilter = new SelectList(await _purchaseOrderRepository.GetPurchaseOrdersFilters(), "PurchaseOrderId", "PurchaseOrderNumber", SortOrder.Ascending);
                TempData["WarningMessage"] = "Sorry, the PO number has been created!";
                return View(model);
            }
        }

        [HttpGet]
        [Authorize(Roles = "UpdateReceiveOrder")]
        public async Task<IActionResult> DetailReceiveOrder(Guid Id)
        {
            ViewBag.Active = "ReceiveOrder";
            ViewBag.PO = new SelectList(await _purchaseOrderRepository.GetPurchaseOrders(), "PurchaseOrderId", "PurchaseOrderNumber", SortOrder.Ascending);
            ViewBag.User = new SelectList(_userManager.Users, nameof(ApplicationUser.Id), nameof(ApplicationUser.NamaUser), SortOrder.Ascending);

            var receiveOrder = await _receiveOrderRepository.GetReceiveOrderById(Id);

            if (receiveOrder == null)
            {
                Response.StatusCode = 404;
                return View("ReceiveOrderNotFound", Id);
            }

            ReceiveOrder model = new ReceiveOrder
            {
                ReceiveOrderId = receiveOrder.ReceiveOrderId,
                ReceiveOrderNumber = receiveOrder.ReceiveOrderNumber,
                PurchaseOrderId = receiveOrder.PurchaseOrderId,
                ReceiveById = receiveOrder.ReceiveById,
                ShippingNumber = receiveOrder.ShippingNumber,
                DeliveryServiceName = receiveOrder.DeliveryServiceName,
                DeliveryDate = receiveOrder.DeliveryDate,
                WaybillNumber = receiveOrder.WaybillNumber,
                InvoiceNumber = receiveOrder.InvoiceNumber,
                SenderName = receiveOrder.SenderName,
                Status = receiveOrder.Status,
                Note = receiveOrder.Note,
                ReceiveOrderDetails = receiveOrder.ReceiveOrderDetails,
            };
            return View(model);
        }

        [Authorize(Roles = "PreviewReceiveOrder")]
        public async Task<IActionResult> PreviewReceiveOrder(Guid Id)
        {
            var rcvOrder = await _receiveOrderRepository.GetReceiveOrderById(Id);

            var CreateDate = rcvOrder.CreateDateTime.ToString("dd MMMM yyyy");
            var RoNumber = rcvOrder.ReceiveOrderNumber;
            var PoNumber = rcvOrder.PurchaseOrder.PurchaseOrderNumber;
            var ShippNumber = rcvOrder.ShippingNumber;
            var WaybillNumber = rcvOrder.WaybillNumber;
            var InvoiceNumber = rcvOrder.InvoiceNumber;
            var DeliveryDate = rcvOrder.DeliveryDate;
            var DeliveryService = rcvOrder.DeliveryServiceName;
            var CreateBy = rcvOrder.ApplicationUser.NamaUser;
            var SenderName = rcvOrder.SenderName;
            var Note = rcvOrder.Note;

            // Path logo untuk QR code
            var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.png");

            // Generate QR Code dengan logo
            var qrCodeImage = GenerateQRCodeWithLogo(RoNumber, logoPath);

            // Simpan QR Code ke dalam MemoryStream sebagai PNG
            using var qrCodeStream = new MemoryStream();
            qrCodeImage.Save(qrCodeStream, System.Drawing.Imaging.ImageFormat.Png);
            qrCodeStream.Position = 0;

            // Load laporan ke FastReport
            WebReport web = new WebReport();
            var reportPath = $"{_webHostEnvironment.WebRootPath}/Reporting/ReceiveOrder.frx";
            web.Report.Load(reportPath);

            // Tambahkan data QR Code sebagai PictureObject
            var pictureObject = web.Report.FindObject("Picture1") as FastReport.PictureObject;
            if (pictureObject != null)
            {
                pictureObject.Image = Image.FromStream(qrCodeStream);
            }

            var msSqlDataConnection = new MsSqlDataConnection();
            msSqlDataConnection.ConnectionString = _configuration.GetConnectionString("DefaultConnection");
            var Conn = msSqlDataConnection.ConnectionString;

            // Set parameter untuk laporan
            web.Report.SetParameterValue("Conn", Conn);
            web.Report.SetParameterValue("CreateDate", CreateDate);
            web.Report.SetParameterValue("ReceiveOrderId", Id.ToString());
            web.Report.SetParameterValue("RoNumber", RoNumber);
            web.Report.SetParameterValue("PoNumber", PoNumber);
            web.Report.SetParameterValue("ShippNumber", ShippNumber);
            web.Report.SetParameterValue("WaybillNumber", WaybillNumber);
            web.Report.SetParameterValue("InvoiceNumber", InvoiceNumber);
            web.Report.SetParameterValue("DeliveryDate", DeliveryDate);
            web.Report.SetParameterValue("DeliveryService", DeliveryService);
            web.Report.SetParameterValue("CreateBy", CreateBy);
            web.Report.SetParameterValue("SenderName", SenderName);
            web.Report.SetParameterValue("Note", Note);            
         
            Stream stream = new MemoryStream();

            web.Report.Prepare();
            web.Report.Export(new PDFSimpleExport(), stream);
            stream.Position = 0;

            return File(stream, "application/pdf");
        }

        [Authorize(Roles = "DownloadReceiveOrder")]
        public async Task<IActionResult> DownloadReceiveOrder(Guid Id)
        {
            var rcvOrder = await _receiveOrderRepository.GetReceiveOrderById(Id);

            var CreateDate = rcvOrder.CreateDateTime.ToString("dd MMMM yyyy");
            var RoNumber = rcvOrder.ReceiveOrderNumber;
            var PoNumber = rcvOrder.PurchaseOrder.PurchaseOrderNumber;
            var ShippNumber = rcvOrder.ShippingNumber;
            var WaybillNumber = rcvOrder.WaybillNumber;
            var InvoiceNumber = rcvOrder.InvoiceNumber;
            var DeliveryDate = rcvOrder.DeliveryDate;
            var DeliveryService = rcvOrder.DeliveryServiceName;
            var CreateBy = rcvOrder.ApplicationUser.NamaUser;
            var SenderName = rcvOrder.SenderName;
            var Note = rcvOrder.Note;

            // Path logo untuk QR code
            var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.png");

            // Generate QR Code dengan logo
            var qrCodeImage = GenerateQRCodeWithLogo(RoNumber, logoPath);

            // Simpan QR Code ke dalam MemoryStream sebagai PNG
            using var qrCodeStream = new MemoryStream();
            qrCodeImage.Save(qrCodeStream, System.Drawing.Imaging.ImageFormat.Png);
            qrCodeStream.Position = 0;

            WebReport web = new WebReport();
            var path = $"{_webHostEnvironment.WebRootPath}\\Reporting\\ReceiveOrder.frx";
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
            web.Report.SetParameterValue("CreateDate", CreateDate);
            web.Report.SetParameterValue("ReceiveOrderId", Id.ToString());
            web.Report.SetParameterValue("RoNumber", RoNumber);
            web.Report.SetParameterValue("PoNumber", PoNumber);
            web.Report.SetParameterValue("ShippNumber", ShippNumber);
            web.Report.SetParameterValue("WaybillNumber", WaybillNumber);
            web.Report.SetParameterValue("InvoiceNumber", InvoiceNumber);
            web.Report.SetParameterValue("DeliveryDate", DeliveryDate);
            web.Report.SetParameterValue("DeliveryService", DeliveryService);
            web.Report.SetParameterValue("CreateBy", CreateBy);
            web.Report.SetParameterValue("SenderName", SenderName);
            web.Report.SetParameterValue("Note", Note);

            web.Report.Prepare();

            Stream stream = new MemoryStream();
            web.Report.Export(new PDFSimpleExport(), stream);
            stream.Position = 0;

            return File(stream, "application/zip", (RoNumber + ".pdf"));
        }

        [Authorize(Roles = "PreviewBatchNumber")]
        public async Task<IActionResult> PreviewBatchNumber(Guid Id)
        {
            var rcvOrder = await _receiveOrderRepository.GetReceiveOrderById(Id);           
            var RoNumber = rcvOrder.ReceiveOrderNumber;

            // Path logo untuk QR code
            var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.png");

            // Generate QR Code dengan logo
            var qrCodeImage = GenerateQRCodeWithLogo(RoNumber, logoPath);

            // Simpan QR Code ke dalam MemoryStream sebagai PNG
            using var qrCodeStream = new MemoryStream();
            qrCodeImage.Save(qrCodeStream, System.Drawing.Imaging.ImageFormat.Png);
            qrCodeStream.Position = 0;

            // Load laporan ke FastReport
            WebReport web = new WebReport();
            var reportPath = $"{_webHostEnvironment.WebRootPath}/Reporting/BatchNumber.frx";
            web.Report.Load(reportPath);

            // Tambahkan data QR Code sebagai PictureObject
            var pictureObject = web.Report.FindObject("Picture1") as FastReport.PictureObject;
            if (pictureObject != null)
            {
                pictureObject.Image = Image.FromStream(qrCodeStream);
            }

            var msSqlDataConnection = new MsSqlDataConnection();
            msSqlDataConnection.ConnectionString = _configuration.GetConnectionString("DefaultConnection");
            var Conn = msSqlDataConnection.ConnectionString;

            // Set parameter untuk laporan
            web.Report.SetParameterValue("Conn", Conn);
            web.Report.SetParameterValue("ReceiveOrderId", Id.ToString());
            web.Report.SetParameterValue("RoNumber", RoNumber);            

            Stream stream = new MemoryStream();

            web.Report.Prepare();
            web.Report.Export(new PDFSimpleExport(), stream);
            stream.Position = 0;

            return File(stream, "application/pdf");
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
