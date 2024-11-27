using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PurchasingSystemStaging.Data;

namespace PurchasingSystemStaging.Areas.Report.Controllers
{
    [Area("Report")]
    [Route("Report/[Controller]/[Action]")]
    public class ReportPurchaseOrderController : Controller
    {
        private readonly ApplicationDbContext _applicationDbContext;

        public ReportPurchaseOrderController(
            ApplicationDbContext applicationDbContext
        )
        {           
            _applicationDbContext = applicationDbContext;            
        }

        public async Task<IActionResult> Index(int? month, int? year)
        {
            // Default ke bulan dan tahun saat ini jika tidak ada input
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var selectedMonth = month ?? currentMonth;
            var selectedYear = year ?? currentYear;

            var orders = await _applicationDbContext.PurchaseOrders
                .Include (t => t.TermOfPayment)
                .Where(po => po.CreateDateTime.Month == selectedMonth && po.CreateDateTime.Year == selectedYear)
                .ToListAsync();

            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.Months = Enumerable.Range(1, 12)
                .Select(i => new { Value = i, Text = new DateTime(currentYear, i, 1).ToString("MMMM") })
                .ToList();
            ViewBag.Years = Enumerable.Range(currentYear - 10, 11).ToList(); // 10 tahun ke belakang

            ViewBag.GrandTotal = orders.Sum(o => o.GrandTotal);

            return View(orders);
        }

    }
}
