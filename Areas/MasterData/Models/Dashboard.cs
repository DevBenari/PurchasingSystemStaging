using PurchasingSystemStaging.Areas.MasterData.ViewModels;
using PurchasingSystemStaging.Models;

namespace PurchasingSystemStaging.Areas.MasterData.Models
{
    public class Dashboard
    {
        public IEnumerable<ApplicationUser> UserOnlines { get; set; }
        public UserActiveViewModel UserActiveViewModels { get; set; }
        public IEnumerable<Product> Products { get; set; }
    }
}
