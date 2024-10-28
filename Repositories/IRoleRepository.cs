using Microsoft.AspNetCore.Identity;

namespace PurchasingSystemStaging.Repositories
{
    public interface IRoleRepository
    {
        ICollection<IdentityRole> GetRoles();
    }
}
