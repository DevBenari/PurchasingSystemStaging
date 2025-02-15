using PurchasingSystem.Repositories;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PurchasingSystem.Areas.Administrator.Models
{
    [Table("AspNetGroupUser", Schema = "dbo")]
    public class GroupUser : UserActivity
    {
        [Key]
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string DepartemenId { get; set; }
    }
}
