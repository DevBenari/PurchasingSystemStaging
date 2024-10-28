﻿using PurchasingSystemStaging.Areas.MasterData.Models;
using PurchasingSystemStaging.Areas.Transaction.Models;
using PurchasingSystemStaging.Models;
using PurchasingSystemStaging.Repositories;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PurchasingSystemStaging.Areas.Warehouse.Models
{
    [Table("WrhWarehouseRequest", Schema = "dbo")]
    public class WarehouseRequest : UserActivity
    {
        [Key]
        public Guid WarehouseRequestId { get; set; }
        public string WarehouseRequestNumber { get; set; }
        public Guid? UnitRequestId { get; set; }
        public string UnitRequestNumber { get; set; }
        public string UserAccessId { get; set; }
        public Guid? UnitLocationId { get; set; }
        public Guid? UnitRequestManagerId { get; set; }
        public Guid? WarehouseLocationId { get; set; }
        public Guid? WarehouseApprovalId { get; set; }
        public string Status { get; set; }
        public int QtyTotal { get; set; }
        public List<WarehouseRequestDetail> WarehouseRequestDetails { get; set; } = new List<WarehouseRequestDetail>();

        //Relationship
        [ForeignKey("UnitRequestId")]
        public UnitRequest? UnitRequest { get; set; }
        [ForeignKey("UserAccessId")]
        public ApplicationUser? ApplicationUser { get; set; }
        [ForeignKey("UnitLocationId")]
        public UnitLocation? UnitLocation { get; set; }
        [ForeignKey("UnitRequestManagerId")]
        public UserActive? UnitRequestManager { get; set; }
        [ForeignKey("WarehouseLocationId")]
        public WarehouseLocation? WarehouseLocation { get; set; }
        [ForeignKey("WarehouseApprovalId")]
        public UserActive? WarehouseApproval { get; set; }
    }

    [Table("WrhWarehouseRequestDetail", Schema = "dbo")]
    public class WarehouseRequestDetail : UserActivity
    {
        [Key]
        public Guid WarehouseRequestDetailId { get; set; }
        public Guid? WarehouseRequestId { get; set; }
        public string ProductNumber { get; set; }
        public string ProductName { get; set; }
        public string Measurement { get; set; }
        public string Supplier { get; set; }
        public int Qty { get; set; }
        public int QtySent { get; set; }
        public bool Checked { get; set; }

        //Relationship
        [ForeignKey("WarehouseRequestId")]
        public WarehouseRequest? WarehouseRequest { get; set; }
    }
}
