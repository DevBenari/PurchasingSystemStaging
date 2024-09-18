using PurchasingSystemApps.Areas.MasterData.Models;
using PurchasingSystemApps.Areas.Order.Models;
using PurchasingSystemApps.Models;
using PurchasingSystemApps.Repositories;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PurchasingSystemApps.Areas.Warehouse.Models
{
    [Table("WrhQtyDifference", Schema = "dbo")]
    public class QtyDifference : UserActivity
    {
        [Key]
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

        //Relationship
        [ForeignKey("PurchaseOrderId")]
        public PurchaseOrder? PurchaseOrder { get; set; }
        [ForeignKey("CheckedById")]
        public ApplicationUser? ApplicationUser { get; set; }
        [ForeignKey("HeadWarehouseManagerId")]
        public UserActive? HeadWarehouseManager { get; set; }
        [ForeignKey("HeadPurchasingManagerId")]
        public UserActive? HeadPurchasingManager { get; set; }
    }

    [Table("WrhQtyDifferenceDetail", Schema = "dbo")]
    public class QtyDifferenceDetail : UserActivity
    {
        [Key]
        public Guid QtyDifferenceDetailId { get; set; }
        public Guid? QtyDifferenceId { get; set; }
        public string ProductNumber { get; set; }
        public string ProductName { get; set; }
        public string Measure { get; set; }
        public int QtyOrder { get; set; }
        public int QtyReceive { get; set; }

        //Relationship        
        [ForeignKey("QtyDifferenceId")]
        public QtyDifference? QtyDifference { get; set; }
    }
}
