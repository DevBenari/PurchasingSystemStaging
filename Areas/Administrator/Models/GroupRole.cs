using PurchasingSystem.Repositories;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PurchasingSystem.Areas.Administrator.Models
{
    [Table("AspNetGroupRole", Schema = "dbo")]
    public class GroupRole : UserActivity
    {
        [Key]
        public Guid Id { get; set; }
        public string RoleId { get; set; }
        public string DepartemenId { get; set; }
    }
}
