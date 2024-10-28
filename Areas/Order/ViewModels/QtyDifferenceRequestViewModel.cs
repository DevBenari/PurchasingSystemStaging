﻿using PurchasingSystemStaging.Areas.Order.Models;
using PurchasingSystemStaging.Areas.Warehouse.Models;

namespace PurchasingSystemStaging.Areas.Order.ViewModels
{
    public class QtyDifferenceRequestViewModel
    {
        public Guid QtyDifferenceRequestId { get; set; }
        public Guid QtyDifferenceId { get; set; }
        public Guid? PurchaseOrderId { get; set; }
        public string UserAccessId { get; set; } //Dibuat Oleh
        public DateTime QtyDifferenceApproveDate { get; set; }
        public string QtyDifferenceApproveBy { get; set; }
        public Guid? HeadWarehouseManagerId { get; set; }
        public Guid? HeadPurchasingManagerId { get; set; }
        public string Status { get; set; }
        public string? Note { get; set; }
        public List<QtyDifferenceDetail> QtyDifferenceDetails { get; set; } = new List<QtyDifferenceDetail>();
    }
}
