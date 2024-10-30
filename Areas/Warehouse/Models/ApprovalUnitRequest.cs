using PurchasingSystemStaging.Areas.MasterData.Models;
using PurchasingSystemStaging.Areas.Order.Models;
using PurchasingSystemStaging.Areas.Transaction.Models;
using PurchasingSystemStaging.Models;
using PurchasingSystemStaging.Repositories;
using System.ComponentModel.DataAnnotations.Schema;

namespace PurchasingSystemStaging.Areas.Warehouse.Models
{
    [Table("WrhApprovalUnitRequest", Schema = "dbo")]
    public class ApprovalUnitRequest : UserActivity
    {
        public Guid ApprovalUnitRequestId { get; set; }
        public Guid? UnitRequestId { get; set; }
        public string UnitRequestNumber { get; set; }
        public string UserAccessId { get; set; } //Dibuat Oleh
        public Guid? UnitLocationId { get; set; }
        public Guid? WarehouseLocationId { get; set; }
        public Guid? UserApproveId { get; set; }
        public string ApproveBy { get; set; }
        public string? ApprovalTime { get; set; }
        public DateTimeOffset ApprovalDate { get; set; }
        public string? ApprovalStatusUser { get; set; }
        public string Status { get; set; }
        public string? Note { get; set; }
        public string? Message { get; set; }

        //Relationship
        [ForeignKey("UnitRequestId")]
        public UnitRequest? UnitRequest { get; set; }        
        [ForeignKey("UnitLocationId")]
        public UnitLocation? UnitLocation { get; set; }
        [ForeignKey("WarehouseLocationId")]
        public WarehouseLocation? WarehouseLocation { get; set; }        
        [ForeignKey("UserAccessId")]
        public ApplicationUser? ApplicationUser { get; set; }
        [ForeignKey("UserApproveId")]
        public UserActive? UserApprove { get; set; }
    }
}
