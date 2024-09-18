using Microsoft.CodeAnalysis;
using PurchasingSystemApps.Repositories;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PurchasingSystemApps.Areas.MasterData.Models
{
    [Table("MstInitialStock", Schema = "dbo")]
    public class InitialStock : UserActivity
    {
        [Key]
        public Guid InitialStockId { get; set; }
        public string GenerateBy { get; set; }
        public Guid? ProductId { get; set; }
        public string? ProductName { get; set; }
        public Guid? PrincipalId { get; set; }
        public string? PrincipalName { get; set; }
        public Guid? LeadTimeId { get; set; }
        public string CalculateBaseOn { get; set; }
        public int MaxRequest { get; set; }
        public int AverageRequest { get; set; }

        //Relationship        
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
        [ForeignKey("PrincipalId")]
        public Principal? Principal { get; set; }
        [ForeignKey("LeadTimeId")]
        public LeadTime? LeadTime { get; set; }
    }
}