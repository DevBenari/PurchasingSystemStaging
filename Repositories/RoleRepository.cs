using Microsoft.AspNetCore.Identity;
using PurchasingSystemStaging.Data;
using PurchasingSystemStaging.Models;

namespace PurchasingSystemStaging.Repositories
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
