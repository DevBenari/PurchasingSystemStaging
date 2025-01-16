﻿using PurchasingSystem.Areas.MasterData.Models;
using PurchasingSystem.Areas.Transaction.Models;
using PurchasingSystem.Models;
using PurchasingSystem.Repositories;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PurchasingSystem.Areas.Warehouse.Models
{
    [Table("WrhWarehouseTransfer", Schema = "dbo")]
    public class WarehouseTransfer : UserActivity
    {
        [Key]
        public Guid WarehouseTransferId { get; set; }
        public string WarehouseTransferNumber { get; set; }
        public Guid? UnitOrderId { get; set; }
        public string UnitOrderNumber { get; set; }
        public string UserAccessId { get; set; }
        public Guid? UnitLocationId { get; set; }
        public Guid? WarehouseLocationId { get; set; }
        public Guid? UserApprove1Id { get; set; }
        public string Status { get; set; }
        public int QtyTotal { get; set; }
        public List<WarehouseTransferDetail> WarehouseTransferDetails { get; set; } = new List<WarehouseTransferDetail>();

        //Relationship
        [ForeignKey("WarehouseRequestId")]
        public UnitOrder? WarehouseRequest { get; set; }
        [ForeignKey("UserAccessId")]
        public ApplicationUser? ApplicationUser { get; set; }
        [ForeignKey("UnitLocationId")]
        public UnitLocation? UnitLocation { get; set; }
        [ForeignKey("WarehouseLocationId")]
        public WarehouseLocation? WarehouseLocation { get; set; }
        [ForeignKey("UserApprove1Id")]
        public UserActive? UserApprove1 { get; set; }
    }

    [Table("WrhWarehouseTransferDetail", Schema = "dbo")]
    public class WarehouseTransferDetail : UserActivity
    {
        [Key]
        public Guid WarehouseTransferDetailId { get; set; }
        public Guid? WarehouseTransferId { get; set; }
        public string ProductNumber { get; set; }
        public string ProductName { get; set; }
        public string Measurement { get; set; }
        public string Supplier { get; set; }
        public int Qty { get; set; }
        public int QtySent { get; set; }
        public bool Checked { get; set; }

        //Relationship
        [ForeignKey("WarehouseTransferId")]
        public WarehouseTransfer? WarehouseTransfer { get; set; }
    }
}
