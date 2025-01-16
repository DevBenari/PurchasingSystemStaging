using FastReport.Data;
using FastReport.Export.PdfSimple;
using FastReport.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PurchasingSystem.Areas.MasterData.Repositories;
using PurchasingSystem.Areas.Order.Repositories;
using PurchasingSystem.Areas.Warehouse.Models;
using PurchasingSystem.Areas.Warehouse.Repositories;
using PurchasingSystem.Areas.Warehouse.ViewModels;
using PurchasingSystem.Data;
using PurchasingSystem.Models;
using PurchasingSystem.Repositories;
using QRCoder;
using System.Data.SqlClient;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace PurchasingSystem.Areas.Warehouse.Controllers
{
    [Area("Warehouse")]
    [Route("Warehouse/[Controller]/[Action]")]
    public class ProductReturnController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IProductReturnRepository _productReturnRepository;
        private readonly IProductRepository _productRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IPositionRepository _positionRepository;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IApprovalProductReturnRepository _approvalProductReturnRepository;
        private readonly IWarehouseLocationRepository _warehouseLocationRepository;

        private readonly IDataProtector _protector;
        private readonly UrlMappingService _urlMappingService;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        public ProductReturnController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IUserActiveRepository userActiveRepository,
            IProductReturnRepository productReturnRepository,
            IProductRepository productRepository,
            IDepartmentRepository departmentRepository,
            IPositionRepository positionRepository,
            IPurchaseOrderRepository purchaseOrderRepository,
            IApprovalProductReturnRepository approvalProductReturnRepository,
            IWarehouseLocationRepository warehouseLocationRepository,

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
            _userActiveRepository = userActiveRepository;
            _productReturnRepository = productReturnRepository;
            _productRepository = productRepository;
            _departmentRepository = departmentRepository;
            _positionRepository = positionRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
            _approvalProductReturnRepository = approvalProductReturnRepository;
            _warehouseLocationRepository = warehouseLocationRepository;

            _protector = provider.CreateProtector("UrlProtector");
            _urlMappingService = urlMappingService;
            _hostingEnvironment = hostingEnvironment;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
        }
        
        public JsonResult LoadProduk(Guid Id)
        {
            var produk = _applicationDbContext.Products.Include(p => p.Supplier).Include(s => s.Measurement).Include(s => s.WarehouseLocation).Include(d => d.Discount).Where(p => p.ProductId == Id).FirstOrDefault();
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
        [Authorize(Roles = "ReadProductReturn")]
        public async Task<IActionResult> Index(string filterOptions = "", string searchTerm = "", DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, int page = 1, int pageSize = 10)
        {
            ViewBag.Active = "ProductReturn";
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
                var data = await _productReturnRepository.GetAllProductReturnPageSize(searchTerm, page, pageSize, startDate, endDate);

                foreach (var item in data.ProductReturns)
                {
                    var updateData = _productReturnRepository.GetAllProductReturn().Where(u => u.ProductReturnId == item.ProductReturnId).FirstOrDefault();
                }

                var model = new Pagination<ProductReturn>
                {
                    Items = data.ProductReturns,
                    TotalCount = data.totalCountProductReturns,
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
                var data = await _productReturnRepository.GetAllProductReturnPageSize(searchTerm, page, pageSize, startDate, endDate);

                foreach (var item in data.ProductReturns)
                {
                    var updateData = _productReturnRepository.GetAllProductReturn().Where(u => u.ProductReturnId == item.ProductReturnId).FirstOrDefault();
                }

                var model = new Pagination<ProductReturn>
                {
                    Items = data.ProductReturns.Where(u => u.CreateBy.ToString() == getUserLogin.Id).ToList(),
                    TotalCount = data.totalCountProductReturns,
                    PageSize = pageSize,
                    CurrentPage = page,
                };

                return View(model);
            }
        }
        
        [HttpGet]
        [Authorize(Roles = "CreateProductReturn")]
        public async Task<IActionResult> CreateProductReturn()
        {
            ViewBag.Active = "ProductReturn";

            _signInManager.IsSignedIn(User);
            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            
            ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
            ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
            ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
            ViewBag.WarehouseLocation = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);

            var productReturn = new ProductReturn()
            {
                UserAccessId = getUser.Id,
            };
            productReturn.ProductReturnDetails.Add(new ProductReturnDetail() { ProductReturnDetailId = Guid.NewGuid() });

            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _productReturnRepository.GetAllProductReturn().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.ProductReturnNumber).FirstOrDefault();
            if (lastCode == null)
            {
                productReturn.ProductReturnNumber = "PRN" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.ProductReturnNumber.Substring(2, 6);

                if (lastCodeTrim != setDateNow)
                {
                    productReturn.ProductReturnNumber = "PRN" + setDateNow + "0001";
                }
                else
                {
                    productReturn.ProductReturnNumber = "PRN" + setDateNow + (Convert.ToInt32(lastCode.ProductReturnNumber.Substring(9, lastCode.ProductReturnNumber.Length - 9)) + 1).ToString("D4");
                }
            }

            return View(productReturn);
        }

        [HttpPost]
        [Authorize(Roles = "CreateProductReturn")]
        public async Task<IActionResult> CreateProductReturn(ProductReturn model)
        {
            ViewBag.Active = "ProductReturn";

            var dateNow = DateTimeOffset.Now;
            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

            var lastCode = _productReturnRepository.GetAllProductReturn().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.ProductReturnNumber).FirstOrDefault();
            if (lastCode == null)
            {
                model.ProductReturnNumber = "PRN" + setDateNow + "0001";
            }
            else
            {
                var lastCodeTrim = lastCode.ProductReturnNumber.Substring(2, 6);

                if (lastCodeTrim != setDateNow)
                {
                    model.ProductReturnNumber = "PRN" + setDateNow + "0001";
                }
                else
                {
                    model.ProductReturnNumber = "PRN" + setDateNow + (Convert.ToInt32(lastCode.ProductReturnNumber.Substring(9, lastCode.ProductReturnNumber.Length - 9)) + 1).ToString("D4");
                }
            }

            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            if (ModelState.IsValid)
            {
                var prn = new ProductReturn
                {
                    CreateDateTime = DateTimeOffset.Now,
                    CreateBy = new Guid(getUser.Id), //Convert Guid to String
                    ProductReturnId = model.ProductReturnId,
                    ProductReturnNumber = model.ProductReturnNumber,
                    ReturnDate = model.ReturnDate,
                    UserAccessId = getUser.Id,
                    BatchNumber = model.BatchNumber,
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
                    Status = model.Status,
                    ReasonForReturn = model.ReasonForReturn,
                    Note = model.Note,
                    MessageApprove1 = model.MessageApprove1,
                    MessageApprove2 = model.MessageApprove2,
                    MessageApprove3 = model.MessageApprove3
                };

                var ItemsList = new List<ProductReturnDetail>();

                foreach (var item in model.ProductReturnDetails)
                {
                    ItemsList.Add(new ProductReturnDetail
                    {
                        CreateDateTime = DateTimeOffset.Now,
                        CreateBy = new Guid(getUser.Id),
                        ProductNumber = item.ProductNumber,
                        ProductName = item.ProductName,
                        Measurement = item.Measurement,
                        WarehouseOrigin = item.WarehouseOrigin,
                        WarehouseExpired = item.WarehouseExpired,
                        Supplier = item.Supplier,
                        Qty = item.Qty,
                        Price = Math.Truncate(item.Price),
                        Discount = item.Discount,
                        SubTotal = Math.Truncate(item.SubTotal)
                    });
                }

                var getItemData = ItemsList.Where(a => a.Supplier != null).FirstOrDefault();

                prn.ProductReturnDetails = ItemsList;
                _productReturnRepository.Tambah(prn);

                ////Signal R
                //var data2 = _ProductReturnRepository.GetAllProductReturn();
                //int totalKaryawan = data2.Count();
                //await _hubContext.Clients.All.SendAsync("UpdateDataCount", totalKaryawan);
                ////End Signal R

                if (model.UserApprove1Id != null)
                {
                    var approval = new ApprovalProductReturn
                    {
                        CreateDateTime = DateTimeOffset.Now,
                        CreateBy = new Guid(getUser.Id),
                        ProductReturnId = prn.ProductReturnId,
                        ProductReturnNumber = prn.ProductReturnNumber,
                        UserAccessId = getUser.Id.ToString(),
                        UserApproveId = prn.UserApprove1Id,
                        ApproveBy = "",
                        ApprovalTime = "",
                        ApprovalDate = DateTimeOffset.MinValue,
                        ApprovalStatusUser = "User1",
                        Status = prn.Status,
                        Note = prn.Note,
                        Message = prn.MessageApprove1
                    };
                    _approvalProductReturnRepository.Tambah(approval);
                }

                if (model.UserApprove2Id != null)
                {
                    var approval = new ApprovalProductReturn
                    {
                        CreateDateTime = DateTimeOffset.Now,
                        CreateBy = new Guid(getUser.Id),
                        ProductReturnId = prn.ProductReturnId,
                        ProductReturnNumber = prn.ProductReturnNumber,
                        UserAccessId = getUser.Id.ToString(),
                        UserApproveId = prn.UserApprove2Id,
                        ApproveBy = "",
                        ApprovalTime = "",
                        ApprovalDate = DateTimeOffset.MinValue,
                        ApprovalStatusUser = "User2",
                        Status = prn.Status,
                        Note = prn.Note,
                        Message = prn.MessageApprove2
                    };
                    _approvalProductReturnRepository.Tambah(approval);
                }

                if (model.UserApprove3Id != null)
                {
                    var approval = new ApprovalProductReturn
                    {
                        CreateDateTime = DateTimeOffset.Now,
                        CreateBy = new Guid(getUser.Id),
                        ProductReturnId = prn.ProductReturnId,
                        ProductReturnNumber = prn.ProductReturnNumber,
                        UserAccessId = getUser.Id.ToString(),
                        UserApproveId = prn.UserApprove3Id,
                        ApproveBy = "",
                        ApprovalTime = "",
                        ApprovalDate = DateTimeOffset.MinValue,
                        ApprovalStatusUser = "User3",
                        Status = prn.Status,
                        Note = prn.Note,
                        Message = prn.MessageApprove3
                    };
                    _approvalProductReturnRepository.Tambah(approval);
                }

                TempData["SuccessMessage"] = "Number " + model.ProductReturnNumber + " Saved";
                return Json(new { redirectToUrl = Url.Action("Index", "ProductReturn") });
            }
            else
            {                
                ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
                ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
                ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
                ViewBag.WarehouseLocation = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);
                TempData["WarningMessage"] = "Please, input all data !";
                return View(model);
            }
        }
        
        [HttpGet]
        [Authorize(Roles = "UpdateProductReturn")]
        public async Task<IActionResult> DetailProductReturn(Guid Id)
        {
            ViewBag.Active = "ProductReturn";

            ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);            
            ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
            ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
            ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
            ViewBag.WarehouseLocation = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);

            var prn = await _productReturnRepository.GetProductReturnById(Id);

            if (prn == null)
            {
                Response.StatusCode = 404;
                return View("ProductReturnNotFound", Id);
            }

            var model = new ProductReturn
            {
                ProductReturnId = prn.ProductReturnId,
                ProductReturnNumber = prn.ProductReturnNumber,
                BatchNumber = prn.BatchNumber,
                UserAccessId = prn.UserAccessId,
                Department1Id = prn.Department1Id,
                Position1Id = prn.Position1Id,
                UserApprove1Id = prn.UserApprove1Id,
                ApproveStatusUser1 = prn.ApproveStatusUser1,
                Department2Id = prn.Department2Id,
                Position2Id = prn.Position2Id,
                UserApprove2Id = prn.UserApprove2Id,
                ApproveStatusUser2 = prn.ApproveStatusUser2,
                Department3Id = prn.Department3Id,
                Position3Id = prn.Position3Id,
                UserApprove3Id = prn.UserApprove3Id,
                ApproveStatusUser3 = prn.ApproveStatusUser3,
                Status = prn.Status,
                ReasonForReturn = prn.ReasonForReturn,
                Note = prn.Note,
                MessageApprove1 = prn.MessageApprove1,
                MessageApprove2 = prn.MessageApprove2,
                MessageApprove3 = prn.MessageApprove3
            };

            var ItemsList = new List<ProductReturnDetail>();

            foreach (var item in prn.ProductReturnDetails)
            {
                ItemsList.Add(new ProductReturnDetail
                {
                    ProductNumber = item.ProductNumber,
                    ProductName = item.ProductName,
                    Measurement = item.Measurement,
                    WarehouseOrigin = item.WarehouseOrigin,
                    WarehouseExpired = item.WarehouseExpired,
                    Supplier = item.Supplier,
                    Qty = item.Qty,
                    Price = Math.Truncate(item.Price),
                    Discount = item.Discount,
                    SubTotal = Math.Truncate(item.SubTotal)
                });
            }

            model.ProductReturnDetails = ItemsList;
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "UpdateProductReturn")]
        public async Task<IActionResult> DetailProductReturn(ProductReturn model)
        {
            ViewBag.Active = "ProductReturn";

            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            if (ModelState.IsValid)
            {
                var productReturn = _productReturnRepository.GetAllProductReturn().Where(p => p.ProductReturnNumber == model.ProductReturnNumber).FirstOrDefault();
                var approval = _approvalProductReturnRepository.GetAllApproval().Where(p => p.ProductReturnNumber == model.ProductReturnNumber).ToList();

                if (productReturn != null)
                {
                    if (approval != null)
                    {
                        foreach (var item in approval)
                        {
                            var itemApproval = approval.Where(o => o.ProductReturnId == item.ProductReturnId).FirstOrDefault();
                            if (itemApproval != null)
                            {
                                item.Note = model.Note;

                                _applicationDbContext.Entry(itemApproval).State = EntityState.Modified;
                                _applicationDbContext.SaveChanges();
                            }
                            else
                            {                                
                                ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
                                ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
                                ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
                                ViewBag.WarehouseLocation = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);
                                TempData["WarningMessage"] = "Name " + item.ApproveBy + " Not Found !!!";
                                return View(model);
                            }
                        }

                        productReturn.UpdateDateTime = DateTimeOffset.Now;
                        productReturn.UpdateBy = new Guid(getUser.Id);
                        productReturn.UserApprove1Id = model.UserApprove1Id;
                        productReturn.UserApprove2Id = model.UserApprove2Id;
                        productReturn.UserApprove3Id = model.UserApprove3Id;
                        productReturn.MessageApprove1 = model.MessageApprove1;
                        productReturn.MessageApprove2 = model.MessageApprove2;
                        productReturn.MessageApprove3 = model.MessageApprove3;
                        productReturn.Note = model.Note;
                        productReturn.ProductReturnDetails = model.ProductReturnDetails;

                        _productReturnRepository.Update(productReturn);

                        TempData["SuccessMessage"] = "Number " + model.ProductReturnNumber + " Changes saved";
                        return Json(new { redirectToUrl = Url.Action("Index", "ProductReturn") });
                    }
                    else
                    {                        
                        ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
                        ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
                        ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
                        ViewBag.WarehouseLocation = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);
                        TempData["WarningMessage"] = "Number " + model.ProductReturnNumber + " Not Found !!!";
                        return View(model);
                    }
                }
                else
                {                    
                    ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
                    ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
                    ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
                    ViewBag.WarehouseLocation = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);
                    TempData["WarningMessage"] = "Number " + model.ProductReturnNumber + " Already exists !!!";
                    return View(model);
                }
            }
            
            ViewBag.Department = new SelectList(await _departmentRepository.GetDepartments(), "DepartmentId", "DepartmentName", SortOrder.Ascending);
            ViewBag.Position = new SelectList(await _positionRepository.GetPositions(), "PositionId", "PositionName", SortOrder.Ascending);
            ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
            ViewBag.WarehouseLocation = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);
            TempData["WarningMessage"] = "Number " + model.ProductReturnNumber + " Failed saved";
            return Json(new { redirectToUrl = Url.Action("Index", "ProductReturn") });
        }

        [Authorize(Roles = "PreviewProductReturn")]
        public async Task<IActionResult> PreviewProductReturn(Guid Id)
        {
            var ProductReturn = await _productReturnRepository.GetProductReturnById(Id);

            var CreateDate = ProductReturn.CreateDateTime.ToString("dd MMMM yyyy");
            var ProductReturnNumber = ProductReturn.ProductReturnNumber;
            var BatchNumber = ProductReturn.BatchNumber;
            var CreateBy = ProductReturn.ApplicationUser.NamaUser;
            var UserApprove1 = ProductReturn.UserApprove1.FullName;
            var UserApprove2 = ProductReturn.UserApprove2.FullName;
            var UserApprove3 = ProductReturn.UserApprove3.FullName;
            var ReasonForReturn = ProductReturn.ReasonForReturn;
            var Note = ProductReturn.Note;

            // Path logo untuk QR code
            var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.png");

            // Generate QR Code dengan logo
            var qrCodeImage = GenerateQRCodeWithLogo(ProductReturnNumber, logoPath);

            // Simpan QR Code ke dalam MemoryStream sebagai PNG
            using var qrCodeStream = new MemoryStream();
            qrCodeImage.Save(qrCodeStream, System.Drawing.Imaging.ImageFormat.Png);
            qrCodeStream.Position = 0;

            // Load laporan ke FastReport
            WebReport web = new WebReport();
            var path = $"{_webHostEnvironment.WebRootPath}\\Reporting\\ProductReturn.frx";
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
            web.Report.SetParameterValue("ProductReturnId", Id.ToString());
            web.Report.SetParameterValue("ProductReturnNumber", ProductReturnNumber);
            web.Report.SetParameterValue("BatchNumber", BatchNumber);
            web.Report.SetParameterValue("CreateDate", CreateDate);
            web.Report.SetParameterValue("CreateBy", CreateBy);
            web.Report.SetParameterValue("UserApprove1", UserApprove1);
            web.Report.SetParameterValue("UserApprove2", UserApprove2);
            web.Report.SetParameterValue("UserApprove3", UserApprove3);
            web.Report.SetParameterValue("ReasonForReturn", ReasonForReturn);
            web.Report.SetParameterValue("Note", Note);

            Stream stream = new MemoryStream();

            web.Report.Prepare();
            web.Report.Export(new PDFSimpleExport(), stream);
            stream.Position = 0;

            return File(stream, "application/pdf");
        }

        [Authorize(Roles = "DownloadProductReturn")]
        public async Task<IActionResult> DownloadProductReturn(Guid Id)
        {
            var ProductReturn = await _productReturnRepository.GetProductReturnById(Id);

            var CreateDate = ProductReturn.CreateDateTime.ToString("dd MMMM yyyy");
            var ProductReturnNumber = ProductReturn.ProductReturnNumber;
            var BatchNumber = ProductReturn.BatchNumber;
            var CreateBy = ProductReturn.ApplicationUser.NamaUser;
            var UserApprove1 = ProductReturn.UserApprove1.FullName;
            var UserApprove2 = ProductReturn.UserApprove2.FullName;
            var UserApprove3 = ProductReturn.UserApprove3.FullName;
            var ReasonForReturn = ProductReturn.ReasonForReturn;
            var Note = ProductReturn.Note;

            // Path logo untuk QR code
            var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.png");

            // Generate QR Code dengan logo
            var qrCodeImage = GenerateQRCodeWithLogo(ProductReturnNumber, logoPath);

            // Simpan QR Code ke dalam MemoryStream sebagai PNG
            using var qrCodeStream = new MemoryStream();
            qrCodeImage.Save(qrCodeStream, System.Drawing.Imaging.ImageFormat.Png);
            qrCodeStream.Position = 0;

            // Load laporan ke FastReport
            WebReport web = new WebReport();
            var path = $"{_webHostEnvironment.WebRootPath}\\Reporting\\ProductReturn.frx";
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
            web.Report.SetParameterValue("ProductReturnId", Id.ToString());
            web.Report.SetParameterValue("ProductReturnNumber", ProductReturnNumber);
            web.Report.SetParameterValue("BatchNumber", BatchNumber);
            web.Report.SetParameterValue("CreateDate", CreateDate);
            web.Report.SetParameterValue("CreateBy", CreateBy);
            web.Report.SetParameterValue("UserApprove1", UserApprove1);
            web.Report.SetParameterValue("UserApprove2", UserApprove2);
            web.Report.SetParameterValue("UserApprove3", UserApprove3);
            web.Report.SetParameterValue("ReasonForReturn", ReasonForReturn);
            web.Report.SetParameterValue("Note", Note);

            web.Report.Prepare();

            Stream stream = new MemoryStream();
            web.Report.Export(new PDFSimpleExport(), stream);
            stream.Position = 0;

            return File(stream, "application/zip", (ProductReturnNumber + ".pdf"));
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
