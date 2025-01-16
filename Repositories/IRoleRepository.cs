using Microsoft.AspNetCore.Identity;

namespace PurchasingSystem.Repositories
{
    public interface IRoleRepository
    {
        ICollection<IdentityRole> GetRoles();
    }
}
