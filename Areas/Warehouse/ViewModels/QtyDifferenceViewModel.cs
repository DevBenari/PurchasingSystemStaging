using PurchasingSystemApps.Areas.Order.Models;
using PurchasingSystemApps.Areas.Warehouse.Models;

namespace PurchasingSystemApps.Areas.Warehouse.ViewModels
{
    public class QtyDifferenceViewModel
    {
        public Guid QtyDifferenceId { get; set; }
        public string QtyDifferenceNumber { get; set; }
        public Guid? PurchaseOrderId { get; set; }
        public string CheckedById { get; set; }
        public Guid? HeadWarehouseManagerId { get; set; }
        public Guid? HeadPurchasingManagerId { get; set; }
        public string Status { get; set; }
        public string? Note { get; set; }
        public List<QtyDifferenceDetail> QtyDifferenceDetails { get; set; } = new List<QtyDifferenceDetail>();
        public List<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();
    }
}
