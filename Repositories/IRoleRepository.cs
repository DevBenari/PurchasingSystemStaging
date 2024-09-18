using Microsoft.AspNetCore.Identity;

namespace PurchasingSystemApps.Repositories
{
    public interface IRoleRepository
    {
        ICollection<IdentityRole> GetRoles();
    }
}
