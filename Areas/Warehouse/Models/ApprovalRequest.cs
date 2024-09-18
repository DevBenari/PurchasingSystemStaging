using PurchasingSystemApps.Areas.MasterData.Models;
using PurchasingSystemApps.Areas.Order.Models;
using PurchasingSystemApps.Areas.Transaction.Models;
using PurchasingSystemApps.Models;
using PurchasingSystemApps.Repositories;
using System.ComponentModel.DataAnnotations.Schema;

namespace PurchasingSystemApps.Areas.Warehouse.Models
{
    [Table("WrhApprovalRequest", Schema = "dbo")]
    public class ApprovalRequest : UserActivity
    {
        public Guid ApprovalRequestId { get; set; }
        public Guid? UnitRequestId { get; set; }
        public string UnitRequestNumber { get; set; }
        public string UserAccessId { get; set; } //Dibuat Oleh
        public Guid? UnitLocationId { get; set; }
        public Guid? UnitRequestManagerId { get; set; }
        public DateTime ApproveDate { get; set; }
        public Guid? WarehouseApprovalId { get; set; } //Mengetahui        
        public string WarehouseApproveBy { get; set; } //Disetujui Oleh
        public string Status { get; set; }
        public string? Note { get; set; }

        //Relationship
        [ForeignKey("UnitRequestId")]
        public UnitRequest? UnitRequest { get; set; }
        [ForeignKey("UnitLocationId")]
        public UnitLocation? UnitLocation { get; set; }
        [ForeignKey("UnitRequestManagerId")]
        public UserActive? UnitRequestManager { get; set; }
        [ForeignKey("WarehouseApprovalId")]
        public UserActive? WarehouseApproval { get; set; }
        [ForeignKey("UserAccessId")]
        public ApplicationUser? ApplicationUser { get; set; }
    }
}
