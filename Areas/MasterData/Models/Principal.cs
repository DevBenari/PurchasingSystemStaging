using PurchasingSystemApps.Repositories;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PurchasingSystemApps.Areas.MasterData.Models
{
    [Table("MstPrincipal", Schema = "dbo")]
    public class Principal : UserActivity
    {
        [Key]
        public Guid PrincipalId { get; set; }
        public string PrincipalCode { get; set; }
        public string PrincipalName { get; set; }
        public string Address { get; set; }
        public string Handphone { get; set; }
        public string Email { get; set; }
        public string? Note { get; set; }
    }
}
