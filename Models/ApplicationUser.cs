using Microsoft.AspNetCore.Identity;

namespace PurchasingSystemApps.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string KodeUser { get; set; }
        public string NamaUser { get; set; }
    }
}
