﻿using PurchasingSystem.Areas.Order.Models;
using PurchasingSystem.Models;
using PurchasingSystem.Repositories;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PurchasingSystem.Areas.Warehouse.Models
{
    [Table("WrhReceiveOrder", Schema = "dbo")]
    public class ReceiveOrder : UserActivity
    {
        [Key]
        public Guid ReceiveOrderId { get; set; }
        public string ReceiveOrderNumber { get; set; }
        public Guid? PurchaseOrderId { get; set; }
        public string ReceiveById { get; set; }
        public string ShippingNumber { get; set; }
        public string DeliveryServiceName { get; set; }
        public string DeliveryDate { get; set; }
        public string WaybillNumber { get; set; }
        public string InvoiceNumber { get; set; }
        public string SenderName { get; set; }
        public string Status { get; set; }
        public string? Note { get; set; }        
        public List<ReceiveOrderDetail> ReceiveOrderDetails { get; set; } = new List<ReceiveOrderDetail>();
        public List<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();

        //Relationship
        [ForeignKey("PurchaseOrderId")]
        public PurchaseOrder? PurchaseOrder { get; set; }
        [ForeignKey("ReceiveById")]
        public ApplicationUser? ApplicationUser { get; set; }
    }

    [Table("WrhReceiveOrderDetail", Schema = "dbo")]
    public class ReceiveOrderDetail : UserActivity
    {
        [Key]
        public Guid ReceivedOrderDetailId { get; set; }
        public Guid? ReceiveOrderId { get; set; }
        public string ProductNumber { get; set; }
        public string ProductName { get; set; }
        public string Measure { get; set; }
        public string Condition { get; set; } //Good Condition, Damage & Pending Inspection
        public int QtyOrder { get; set; }
        public int QtyReceive { get; set; }

        //Relationship        
        [ForeignKey("ReceiveOrderId")]
        public ReceiveOrder? ReceiveOrder { get; set; }
    }
}
