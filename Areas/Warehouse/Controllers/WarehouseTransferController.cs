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
using PurchasingSystem.Areas.Transaction.Models;
using PurchasingSystem.Areas.Transaction.Repositories;
using PurchasingSystem.Areas.Warehouse.Models;
using PurchasingSystem.Areas.Warehouse.Repositories;
using PurchasingSystem.Areas.Warehouse.ViewModels;
using PurchasingSystem.Data;
using PurchasingSystem.Models;
using PurchasingSystem.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace PurchasingSystem.Areas.Warehouse.Controllers
{
    [Area("Warehouse")]
    [Route("Warehouse/[Controller]/[Action]")]
    public class WarehouseTransferController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IProductRepository _productRepository;
        private readonly ITermOfPaymentRepository _termOfPaymentRepository;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IPurchaseRequestRepository _purchaseRequestRepository;
        private readonly IUnitLocationRepository _unitLocationRepository;
        private readonly IWarehouseLocationRepository _warehouseLocationRepository;
        private readonly IUnitRequestRepository _unitRequestRepository;
        private readonly IWarehouseTransferRepository _warehouseTransferRepository;

        private readonly IDataProtector _protector;
        private readonly UrlMappingService _urlMappingService;


        public WarehouseTransferController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            IUserActiveRepository userActiveRepository,
            IProductRepository productRepository,
            ITermOfPaymentRepository termOfPaymentRepository,
            IPurchaseOrderRepository purchaseOrderRepository,
            IPurchaseRequestRepository purchaseRequestRepository,
            IUnitLocationRepository unitLocationRepository,
            IWarehouseLocationRepository warehouseLocationRepository,
            IUnitRequestRepository unitRequestRepository,
            IWarehouseTransferRepository warehouseTransferRepository,

            IDataProtectionProvider provider,
            UrlMappingService urlMappingService
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _userActiveRepository = userActiveRepository;
            _productRepository = productRepository;
            _termOfPaymentRepository = termOfPaymentRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
            _purchaseRequestRepository = purchaseRequestRepository;
            _unitLocationRepository = unitLocationRepository;
            _warehouseLocationRepository = warehouseLocationRepository;
            _unitRequestRepository = unitRequestRepository;
            _warehouseTransferRepository = warehouseTransferRepository;

            _protector = provider.CreateProtector("UrlProtector");
            _urlMappingService = urlMappingService;
        }
        
        [HttpGet]
        [Authorize(Roles = "ReadWarehouseLocation")]
        public async Task<IActionResult> Index(string filterOptions = "", string searchTerm = "", DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, int page = 1, int pageSize = 10)
        {
            ViewBag.Active = "WarehouseTransfer";
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

            var data = await _warehouseTransferRepository.GetAllWarehouseTransferPageSize(searchTerm, page, pageSize, startDate, endDate);

            var model = new Pagination<WarehouseTransfer>
            {
                Items = data.warehouseTransfers,
                TotalCount = data.totalCountWarehouseTransfers,
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
        [Authorize(Roles = "UpdateWarehouseLocation")]
        public async Task<IActionResult> DetailWarehouseTransfer(Guid Id)
        {
            ViewBag.Active = "WarehouseTransfer";

            ViewBag.User = new SelectList(_userManager.Users, nameof(ApplicationUser.Id), nameof(ApplicationUser.NamaUser), SortOrder.Ascending);
            ViewBag.UnitLocation = new SelectList(await _unitLocationRepository.GetUnitLocations(), "UnitLocationId", "UnitLocationName", SortOrder.Ascending);
            ViewBag.RequestBy = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
            ViewBag.WarehouseLocation = new SelectList(await _warehouseLocationRepository.GetWarehouseLocations(), "WarehouseLocationId", "WarehouseLocationName", SortOrder.Ascending);
            ViewBag.Approval = new SelectList(await _userActiveRepository.GetUserActives(), "UserActiveId", "FullName", SortOrder.Ascending);
            ViewBag.Product = new SelectList(await _productRepository.GetProducts(), "ProductId", "ProductName", SortOrder.Ascending);

            //var WarehouseTransfer = await _warehouseTransferRepository.GetWarehouseTransferById(Id);

            WarehouseTransfer WarehouseTransfer = _applicationDbContext.WarehouseTransfers
                .Include(d => d.WarehouseTransferDetails)
                .Include(u => u.ApplicationUser)
                .Include(p => p.UnitLocation)
                .Include(t => t.WarehouseLocation)
                .Include(y => y.UserApprove1)
                .Where(p => p.WarehouseTransferId == Id).FirstOrDefault();

            if (WarehouseTransfer == null)
            {
                Response.StatusCode = 404;
                return View("WarehouseTransferNotFound", Id);
            }

            WarehouseTransfer model = new WarehouseTransfer
            {
                WarehouseTransferId = WarehouseTransfer.WarehouseTransferId,
                WarehouseTransferNumber = WarehouseTransfer.WarehouseTransferNumber,
                UnitOrderId = WarehouseTransfer.UnitOrderId,
                UnitOrderNumber = WarehouseTransfer.UnitOrderNumber,
                UserAccessId = WarehouseTransfer.UserAccessId,
                UnitLocationId = WarehouseTransfer.UnitLocationId,
                WarehouseLocationId = WarehouseTransfer.WarehouseLocationId,
                UserApprove1Id = WarehouseTransfer.UserApprove1Id,
                QtyTotal = WarehouseTransfer.QtyTotal,
                Status = WarehouseTransfer.Status
            };

            var ItemsList = new List<WarehouseTransferDetail>();

            foreach (var item in WarehouseTransfer.WarehouseTransferDetails)
            {
                ItemsList.Add(new WarehouseTransferDetail
                {
                    ProductNumber = item.ProductNumber,
                    ProductName = item.ProductName,
                    Measurement = item.Measurement,
                    Supplier = item.Supplier,
                    Qty = item.Qty,
                    QtySent = item.QtySent,
                    Checked = item.Checked
                });
            }

            model.WarehouseTransferDetails = ItemsList;
            return View(model);
        }
    }
}
