using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PurchasingSystemStaging.Areas.MasterData.Repositories;
using PurchasingSystemStaging.Areas.Order.Repositories;
using PurchasingSystemStaging.Areas.Report.Models;
using PurchasingSystemStaging.Areas.Report.Repositories;
using PurchasingSystemStaging.Data;
using PurchasingSystemStaging.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace PurchasingSystemStaging.Areas.Report.Controllers
{
    [Area("Report")]
    [Route("Report/[Controller]/[Action]")]
    public class CalculatedPurchaseOrderController : Controller
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IUserActiveRepository _userActiveRepository;
        private readonly IClosingPurchaseOrderRepository _closingPurchaseOrderRepository;

        private readonly IDataProtector _protector;
        private readonly UrlMappingService _urlMappingService;

        public CalculatedPurchaseOrderController(
            ApplicationDbContext applicationDbContext,
            IPurchaseOrderRepository purchaseOrderRepository,
            IUserActiveRepository userActiveRepository,
            IClosingPurchaseOrderRepository closingPurchaseOrderRepository,

            IDataProtectionProvider provider,
            UrlMappingService urlMappingService
        )
        {           
            _applicationDbContext = applicationDbContext;
            _purchaseOrderRepository = purchaseOrderRepository;
            _userActiveRepository = userActiveRepository;
            _closingPurchaseOrderRepository = closingPurchaseOrderRepository;

            _protector = provider.CreateProtector("UrlProtector");
            _urlMappingService = urlMappingService;
        }

        public IActionResult RedirectToIndex(int? month, int? year, string filterOptions = "", string searchTerm = "", DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, int page = 1, int pageSize = 50)
        {
            try
            {
                // Format tanggal tanpa waktu
                string startDateString = startDate.HasValue ? startDate.Value.ToString("yyyy-MM-dd") : "";
                string endDateString = endDate.HasValue ? endDate.Value.ToString("yyyy-MM-dd") : "";

                // Bangun originalPath dengan format tanggal ISO 8601
                string originalPath = $"Page:Report/CalculatedPurchaseOrder/Index?month={month}&year={year}&filterOptions={filterOptions}&searchTerm={searchTerm}&startDate={startDateString}&endDate={endDateString}&page={page}&pageSize={pageSize}";
                string encryptedPath = _protector.Protect(originalPath);
                
                // Hash GUID-like code (SHA256 truncated to 36 characters)
                string guidLikeCode = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(encryptedPath)))
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .Substring(0, 36);

                // Simpan mapping GUID-like code ke encryptedPath di penyimpanan sementara (misalnya, cache)
                _urlMappingService.InMemoryMapping[guidLikeCode] = encryptedPath;

                return Redirect("/" + guidLikeCode);
            }
            catch
            {
                // Jika enkripsi gagal, kembalikan view
                return Redirect(Request.Path);
            }
        }

        public async Task<IActionResult> Index(int? month, int? year, string filterOptions = "", string searchTerm = "", DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, int page = 1, int pageSize = 50)
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

            // Default ke bulan dan tahun saat ini jika tidak ada input
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var selectedMonth = month ?? currentMonth;
            var selectedYear = year ?? currentYear;

            var months = Enumerable.Range(1, 12)
        .Select(i => new SelectListItem
        {
            Value = i.ToString(),
            Text = new DateTime(currentYear, i, 1).ToString("MMMM")
        }).ToList();

            var years = Enumerable.Range(currentYear - 10, 11)
                .Select(year => new SelectListItem
                {
                    Value = year.ToString(),
                    Text = year.ToString()
                }).ToList();


            var data = await _purchaseOrderRepository.GetAllPurchaseOrderPageSize(searchTerm, page, pageSize, startDate, endDate);

            // Hitung Grand Total dari semua data yang sesuai dengan filter
            var filteredOrders = data.purchaseOrders
                .Where(po => po.CreateDateTime.Month == selectedMonth && po.CreateDateTime.Year == selectedYear);

            // Gabungkan data yang sudah di-filter dalam pagination
            var ordersWithSupplier = filteredOrders.Select(po => new PurchaseOrderWithDetailSupplier
            {
                CreateDateTime = po.CreateDateTime,
                CreateBy = po.CreateBy,
                PurchaseOrderId = po.PurchaseOrderId,
                PurchaseOrderNumber = po.PurchaseOrderNumber,
                TermOfPayment = po.TermOfPayment.TermOfPaymentName,
                Status = po.Status,
                QtyTotal = po.QtyTotal,
                GrandTotal = po.GrandTotal,
                SupplierName = po.PurchaseOrderDetails
                    .Select(pod => pod.Supplier).FirstOrDefault()
            }).ToList();

            var model = new Pagination<PurchaseOrderWithDetailSupplier>
            {
                Items = ordersWithSupplier,
                TotalCount = data.totalCountPurchaseOrders,
                PageSize = pageSize,
                CurrentPage = page,

                SelectedMonth = selectedMonth,
                SelectedYear = selectedYear,
                Months = months,
                Years = years
            };

            ViewBag.SelectedMonth = model.SelectedMonth;
            ViewBag.SelectedYear = model.SelectedYear;            

            ViewBag.GrandTotal = filteredOrders.Sum(o => o.GrandTotal);

            return View(model);
        }

        public IActionResult RedirectToClosed(int? month, int? year)
        {
            try
            {
                // Bangun originalPath
                string originalPath = $"Page:Report/CalculatedPurchaseOrder/ClosedPurchaseOrder?month={month}&year={year}";                
                string encryptedPath = _protector.Protect(originalPath);

                // Hash GUID-like code (SHA256 truncated to 36 characters)
                string guidLikeCode = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(encryptedPath)))
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .Substring(0, 36);

                // Simpan mapping GUID-like code ke encryptedPath di penyimpanan sementara (misalnya, cache)
                _urlMappingService.InMemoryMapping[guidLikeCode] = encryptedPath;

                return Json(new { success = true, encryptedUrl = "/" + guidLikeCode });
            }
            catch (Exception ex)
            {
                // Log error jika diperlukan
                Console.WriteLine($"Error: {ex.Message}");

                // Kembalikan JSON untuk fallback
                return Json(new { success = false, message = "Failed to encrypt URL." });
            }
        }

        public async Task<IActionResult> ClosedPurchaseOrder(int? month, int? year)
        {
            ViewBag.Active = "Report";

            var getUser = _userActiveRepository.GetAllUserLogin().Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            // Default ke bulan dan tahun saat ini jika tidak ada input
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var selectedMonth = month ?? currentMonth;
            var selectedYear = year ?? currentYear;

            var data = await _purchaseOrderRepository.GetAllPurchaseOrderPageSize("", 1, 100, null, null);

            // Hitung Grand Total dari semua data yang sesuai dengan filter
            var filteredOrders = data.purchaseOrders
                .Where(po => po.CreateDateTime.Month == selectedMonth && po.CreateDateTime.Year == selectedYear);

            if (ModelState.IsValid)
            {
                if (filteredOrders != null && filteredOrders.Any()) 
                {
                    var checkMonth = _closingPurchaseOrderRepository.GetAllClosingPurchaseOrder().Where(m => m.Month == month && m.Year == year).FirstOrDefault();
                    if (checkMonth == null)
                    {
                        var cpo = new ClosingPurchaseOrder
                        {
                            CreateDateTime = DateTimeOffset.Now,
                            CreateBy = new Guid(getUser.Id),
                            UserAccessId = getUser.Id,
                            Month = selectedMonth,
                            Year = selectedYear,
                            TotalPo = filteredOrders.Count(),
                            TotalQty = filteredOrders.Sum(po => po.QtyTotal),
                            GrandTotal = filteredOrders.Sum(po => po.GrandTotal),
                        };

                        var ItemsList = new List<ClosingPurchaseOrderDetail>();

                        var statusInOrder = filteredOrders.Where(i => i.Status == "In Order").Count();
                        var statusCancelled = filteredOrders.Where(i => i.Status == "Cancelled").Count();

                        if (statusInOrder == 0 && statusCancelled == 0) 
                        {
                            foreach (var item in filteredOrders)
                            {
                                foreach (var pod in item.PurchaseOrderDetails)
                                {
                                    if (item.PurchaseOrderId == pod.PurchaseOrderId && item.Status.StartsWith("RO"))
                                    {
                                        ItemsList.Add(new ClosingPurchaseOrderDetail
                                        {
                                            CreateDateTime = DateTimeOffset.Now,
                                            CreateBy = new Guid(getUser.Id),
                                            PurchaseOrderNumber = item.PurchaseOrderNumber,
                                            TermOfPaymentName = item.TermOfPayment.TermOfPaymentName,
                                            Status = item.Status,
                                            SupplierName = pod.Supplier,
                                            Qty = item.QtyTotal,
                                            TotalPrice = Math.Truncate(pod.SubTotal)
                                        });
                                    }
                                }
                            }

                            cpo.ClosingPurchaseOrderDetails = ItemsList;

                            var dateNow = DateTimeOffset.Now;
                            var setDateNow = DateTimeOffset.Now.ToString("yyMMdd");

                            var lastCode = _closingPurchaseOrderRepository.GetAllClosingPurchaseOrder().Where(d => d.CreateDateTime.ToString("yyMMdd") == dateNow.ToString("yyMMdd")).OrderByDescending(k => k.ClosingPurchaseOrderNumber).FirstOrDefault();
                            if (lastCode == null)
                            {
                                cpo.ClosingPurchaseOrderNumber = "CPO" + setDateNow + "0001";
                            }
                            else
                            {
                                var lastCodeTrim = lastCode.ClosingPurchaseOrderNumber.Substring(2, 6);

                                if (lastCodeTrim != setDateNow)
                                {
                                    cpo.ClosingPurchaseOrderNumber = "CPO" + setDateNow + "0001";
                                }
                                else
                                {
                                    cpo.ClosingPurchaseOrderNumber = "CPO" + setDateNow + (Convert.ToInt32(lastCode.ClosingPurchaseOrderNumber.Substring(9, lastCode.ClosingPurchaseOrderNumber.Length - 9)) + 1).ToString("D4");
                                }
                            }

                            _closingPurchaseOrderRepository.Tambah(cpo);

                            TempData["SuccessMessage"] = "Closed Month " + cpo.Month + " Success...";
                            return RedirectToAction("RedirectToIndex", "CalculatedPurchaseOrder");
                        }
                        else
                        {
                            TempData["WarningMessage"] = "Sorry, there is still a PO status that has not been completed !";
                            return RedirectToAction("RedirectToIndex", "CalculatedPurchaseOrder");
                        }
                    }
                    else
                    {
                        TempData["WarningMessage"] = "Sorry, that month has been closed !";
                        return RedirectToAction("RedirectToIndex", "CalculatedPurchaseOrder");
                    }
                }                
                else 
                {
                    TempData["WarningMessage"] = "Sorry, data this month empty !";
                    return RedirectToAction("RedirectToIndex", "CalculatedPurchaseOrder");
                }
            }

            return View();
        }

        public class PurchaseOrderWithDetailSupplier
        {            
            public DateTimeOffset CreateDateTime { get; set; }
            public Guid CreateBy { get; set; }
            public Guid PurchaseOrderId { get; set; }
            public string PurchaseOrderNumber { get; set; }
            public string TermOfPayment { get; set; }
            public string Status { get; set; }
            public int QtyTotal { get; set; }
            public decimal GrandTotal { get; set; }
            public string SupplierName { get; set; }  // Menyimpan nama supplier            
        }
    }
}
