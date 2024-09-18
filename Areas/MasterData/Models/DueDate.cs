using PurchasingSystemApps.Repositories;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PurchasingSystemApps.Areas.MasterData.Models
{
    [Table("MstDueDate", Schema = "dbo")]
    public class DueDate : UserActivity
    {
        [Key]
        public Guid DueDateId { get; set; }
        public string Value { get; set; }
    }
}
