using PurchasingSystemStaging.Repositories;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PurchasingSystemStaging.Areas.MasterData.Models
{
    [Table("MstTermOfPayment", Schema = "dbo")]
    public class TermOfPayment : UserActivity
    {
        [Key]
        public Guid TermOfPaymentId { get; set; }
        public string TermOfPaymentCode { get; set; }
        public string TermOfPaymentName { get; set; }
        public string? Note { get; set; }
    }
}
