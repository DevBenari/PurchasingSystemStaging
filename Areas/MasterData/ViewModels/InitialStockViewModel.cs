using PurchasingSystemApps.Areas.MasterData.Models;
using System.ComponentModel.DataAnnotations;

namespace PurchasingSystemApps.Areas.MasterData.ViewModels
{
    public class InitialStockViewModel
    {
        public Guid InitialStockId { get; set; }
        [Required(ErrorMessage = "Sorry, please choose!")]
        public string GenerateBy { get; set; }
        [Required(ErrorMessage = "Sorry, please choose!")]
        public Guid? ProductId { get; set; }
        public string? ProductName { get; set; }
        [Required(ErrorMessage = "Sorry, please choose!")]
        public Guid? PrincipalId { get; set; }
        public string? PrincipalName { get; set; }
        [Required(ErrorMessage = "Sorry, please choose!")]
        public Guid? LeadTimeId { get; set; }
        public string CalculateBaseOn { get; set; }
        public int MaxRequest { get; set; }
        public int AverageRequest { get; set; }
    }
}
