using PurchasingSystemStaging.Areas.Order.Models;
using PurchasingSystemStaging.Repositories;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PurchasingSystemStaging.Areas.Report.Models
{
    [Table("RptClosingPurchaseOrder", Schema = "dbo")]
    public class ClosingPurchaseOrder : UserActivity
    {
        [Key]
        public Guid ClosingPoId { get; set; }
        public string ClosingPoNumber { get; set; }
        public string Month { get; set; }
        public string Year { get; set; }
        public decimal GrandTotal { get; set; }
        public List<ClosingPurchaseOrderDetail> ClosingPurchaseOrderDetails { get; set; } = new List<ClosingPurchaseOrderDetail>();
    }

    [Table("RptClosingPurchaseOrderDetail", Schema = "dbo")]
    public class ClosingPurchaseOrderDetail : UserActivity
    {
        [Key]
        public Guid ClosingPoDetailId { get; set; }
        public Guid? ClosingPoId { get; set; }
        public string PurchaseOrderNumber { get; set; }
        public string TermOfPaymentName { get; set; }
        public int QtyTotal { get; set; }
        public string Supplier { get; set; }
        public int Qty { get; set; }
        public decimal GrandTotal { get; set; }

        //Relationship
        [ForeignKey("ClosingPoId")]
        public ClosingPurchaseOrder? ClosingPurchaseOrder { get; set; }
    }
}
