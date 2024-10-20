using PurchasingSystemApps.Areas.MasterData.ViewModels;
using PurchasingSystemApps.Models;

namespace PurchasingSystemApps.Areas.MasterData.Models
{
    public class Dashboard
    {
        public IEnumerable<ApplicationUser> UserOnlines { get; set; }
        public UserActiveViewModel UserActiveViewModels { get; set; }
        public IEnumerable<Product> Products { get; set; }
    }
}
