using Microsoft.AspNetCore.Identity;
using PurchasingSystemApps.Data;
using PurchasingSystemApps.Models;

namespace PurchasingSystemApps.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly ApplicationDbContext _context;
        public RoleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public ICollection<IdentityRole> GetRoles()
        {
            return _context.Roles.ToList();
        }
    }
}
