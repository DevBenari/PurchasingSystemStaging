using PurchasingSystemStaging.Areas.Order.Models;
using PurchasingSystemStaging.Areas.Warehouse.Models;

namespace PurchasingSystemStaging.Areas.Order.ViewModels
{
    public class ApprovalQtyDifferenceViewModel
    {
        public Guid ApprovalQtyDifferenceId { get; set; }
        public Guid? QtyDifferenceId { get; set; }
        public string QtyDifferenceNumber { get; set; }
        public string UserAccessId { get; set; } //Dibuat Oleh
        public Guid? PurchaseOrderId { get; set; }
        public string PurchaseOrderNumber { get; set; }
        public Guid? UserApproveId { get; set; }
        public string? UserApprove { get; set; }
        public string ApproveBy { get; set; }
        public string? ApprovalTime { get; set; }
        public DateTimeOffset ApprovalDate { get; set; }
        public string? ApprovalStatusUser { get; set; }
        public string Status { get; set; }
        public string? Note { get; set; }
        public string? Message { get; set; }
        public List<QtyDifferenceDetail> QtyDifferenceDetails { get; set; }
    }
}
