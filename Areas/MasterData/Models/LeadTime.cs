using PurchasingSystemApps.Repositories;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PurchasingSystemApps.Areas.MasterData.Models
{
    [Table("MstLeadTime", Schema = "dbo")]
    public class LeadTime : UserActivity
    {
        [Key]
        public Guid LeadTimeId { get; set; }
        public string LeadTimeCode { get; set; }
        public int LeadTimeValue { get; set; }
    }
}
