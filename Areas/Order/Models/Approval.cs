using PurchasingSystemApps.Areas.MasterData.Models;
using PurchasingSystemApps.Models;
using PurchasingSystemApps.Repositories;
using System.ComponentModel.DataAnnotations.Schema;

namespace PurchasingSystemApps.Areas.Order.Models
{
    [Table("OrdApproval", Schema = "dbo")]
    public class Approval : UserActivity
    {
        public Guid ApprovalId { get; set; }
        public Guid? PurchaseRequestId { get; set; }
        public string PurchaseRequestNumber { get; set; }
        public string UserAccessId { get; set; } //Dibuat Oleh
        public int ExpiredDay { get; set; }
        public int RemainingDay { get; set; }
        public DateTimeOffset ExpiredDate { get; set; }
        public Guid? UserApproveId { get; set; }
        public string ApproveBy { get; set; }
        public string? ApprovalTime { get; set; }
        public DateTimeOffset ApprovalDate { get; set; }
        public string? ApprovalStatusUser { get; set; }
        public string Status { get; set; }
        public string? Note { get; set; }

        //Relationship
        [ForeignKey("PurchaseRequestId")]
        public PurchaseRequest? PurchaseRequest { get; set; }        
        [ForeignKey("UserAccessId")]
        public ApplicationUser? ApplicationUser { get; set; }
        [ForeignKey("UserApproveId")]
        public UserActive? UserApprove { get; set; }        
        [ForeignKey("DueDateId")]
        public DueDate? DueDate { get; set; }
    }
}
