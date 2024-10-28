﻿using PurchasingSystemStaging.Areas.Transaction.Models;

namespace PurchasingSystemStaging.Areas.Transaction.ViewModels
{
    public class UnitRequestViewModel
    {
        public Guid UnitRequestId { get; set; }
        public string UnitRequestNumber { get; set; }
        public string UserAccessId { get; set; }
        public Guid? UnitLocationId { get; set; }
        public Guid? UnitRequestManagerId { get; set; }
        public Guid? WarehouseLocationId { get; set; }
        public Guid? WarehouseApprovalId { get; set; }
        public int QtyTotal { get; set; }
        public string Status { get; set; }
        public string? Note { get; set; }
        public List<UnitRequestDetail> UnitRequestDetails { get; set; } = new List<UnitRequestDetail>();
    }
}
