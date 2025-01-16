using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PurchasingSystem.Areas.MasterData.Repositories;
using PurchasingSystem.Areas.Order.Repositories;
using PurchasingSystem.Areas.Report.Models;
using PurchasingSystem.Areas.Report.Repositories;
using PurchasingSystem.Data;
using PurchasingSystem.Repositories;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PurchasingSystem.Areas.Report.Controllers
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

        [Authorize(Roles = "ReadCalculatedPurchaseOrder")]
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
            var ordersWithSupplier = filteredOrders.Where(po => po.Status == "In Order" || po.Status.StartsWith("RO")).Select(po => new PurchaseOrderWithDetailSupplier
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

            ViewBag.GrandTotal = ordersWithSupplier.Sum(o => o.GrandTotal);

            return View(model);
        }

        [Authorize(Roles = "ClosedPurchaseOrder")]
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

            var ordersWithSupplier = filteredOrders.Where(po => po.Status == "In Order" || po.Status.StartsWith("RO"));

            if (ModelState.IsValid)
            {
                if (filteredOrders != null && filteredOrders.Any()) 
                {
                    var checkMonthNow = _closingPurchaseOrderRepository.GetAllClosingPurchaseOrder().FirstOrDefault(m => m.Month == selectedMonth && m.Year == selectedYear);

                    // Periksa bulan sebelumnya
                    var previousMonth = selectedMonth == 1 ? 12 : selectedMonth - 1;
                    var previousYear = selectedMonth == 1 ? selectedYear - 1 : selectedYear;

                    // Periksa pada table PO Bulan Sebelumnya
                    var checkMonthPreviousInPO = filteredOrders.Where(po => po.CreateDateTime.Month == previousMonth && po.CreateDateTime.Year == previousYear).FirstOrDefault();

                    var checkMonthPrevious = _closingPurchaseOrderRepository
                        .GetAllClosingPurchaseOrder()
                        .FirstOrDefault(m => m.Month == previousMonth && m.Year == previousYear);

                    // Periksa bulan setelahnya
                    var nextMonth = selectedMonth == 12 ? 1 : selectedMonth + 1;
                    var nextYear = selectedMonth == 12 ? selectedYear + 1 : selectedYear;

                    // Periksa pada table PO Bulan Setelahnya
                    var checkMonthNextInPO = filteredOrders.Where(po => po.CreateDateTime.Month == nextMonth && po.CreateDateTime.Year == nextYear).FirstOrDefault();

                    var checkMonthNext = _closingPurchaseOrderRepository
                        .GetAllClosingPurchaseOrder()
                        .FirstOrDefault(m => m.Month == nextMonth && m.Year == nextYear);

                    // Validasi: Bulan sebelumnya harus sudah di-close, dan bulan setelahnya belum boleh di-close
                    if (checkMonthPreviousInPO != null && checkMonthPrevious == null)
                    {
                        TempData["WarningMessage"] = "Sorry, the previous month has not been closed yet!";
                        return RedirectToAction("Index", "CalculatedPurchaseOrder");
                    }

                    if (checkMonthNextInPO != null && checkMonthNext != null)
                    {
                        TempData["WarningMessage"] = "Sorry, the next month has already been closed!";
                        return RedirectToAction("Index", "CalculatedPurchaseOrder");
                    }

                    //var checkMonthNow = _closingPurchaseOrderRepository.GetAllClosingPurchaseOrder().Where(m => m.Month == month && m.Year == year).FirstOrDefault();

                    if (checkMonthNow == null)
                    {
                        var cpo = new ClosingPurchaseOrder
                        {
                            CreateDateTime = DateTimeOffset.Now,
                            CreateBy = new Guid(getUser.Id),
                            UserAccessId = getUser.Id,
                            Month = selectedMonth,
                            Year = selectedYear,
                            TotalPo = ordersWithSupplier.Count(),
                            TotalQty = ordersWithSupplier.Sum(po => po.QtyTotal),
                            GrandTotal = ordersWithSupplier.Sum(po => po.GrandTotal),
                        };

                        var ItemsList = new List<ClosingPurchaseOrderDetail>();

                        var statusInOrder = ordersWithSupplier.Where(i => i.Status == "In Order").Count();

                        if (statusInOrder == 0) 
                        {
                            foreach (var item in ordersWithSupplier)
                            {
                                var pod = item.PurchaseOrderDetails.Where(p => p.PurchaseOrderId == item.PurchaseOrderId).FirstOrDefault();

                                if (pod != null)
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
                                            TotalPrice = item.GrandTotal
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

                            // Misal cpo.Month = 11
                            string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(cpo.Month);

                            //TempData["SuccessMessage"] = "Closed Month " + cpo.Month + " Success...";
                            return Json(new { success = true, message = $"Closed Month {monthName} Success..." });
                        }
                        else
                        {
                            //TempData["WarningMessage"] = "Sorry, there is still a PO status that has not been completed !";
                            return Json(new { success = false, message = "Sorry, there is still a PO status that has not been completed !" });
                        }
                    }
                    else
                    {
                        //TempData["WarningMessage"] = "Sorry, that month has been closed !";                        
                        return Json(new { success = false, message = "Sorry, that month has been closed !" });

                    }
                }                
                else 
                {
                    //TempData["WarningMessage"] = "Sorry, data this month empty !";
                    return Json(new { success = false, message = "Sorry, data this month empty !" });                    
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
