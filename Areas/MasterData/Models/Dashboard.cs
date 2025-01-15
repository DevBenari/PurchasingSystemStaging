using PurchasingSystem.Areas.MasterData.ViewModels;
using PurchasingSystem.Models;

namespace PurchasingSystem.Areas.MasterData.Models
{
    public class Dashboard
    {
        public IEnumerable<ApplicationUser> UserOnlines { get; set; }
        public UserActiveViewModel UserActiveViewModels { get; set; }
        public IEnumerable<Product> Products { get; set; }
    }
}
