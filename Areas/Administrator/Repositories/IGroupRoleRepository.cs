﻿using Microsoft.EntityFrameworkCore;
using PurchasingSystem.Areas.Administrator.Models;
using PurchasingSystem.Data;

namespace PurchasingSystem.Areas.Administrator.Repositories
{
    public class IGroupRoleRepository
    {
        private readonly ApplicationDbContext _context;

        public IGroupRoleRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public IEnumerable<GroupRole> GetAllGroupRole()
        {
            return _context.GroupRoles
                .AsNoTracking();
        }

        public GroupRole Tambah(GroupRole role)
        {
            _context.GroupRoles.Add(role);
            _context.SaveChanges();
            return role;
        }

        public void DeleteByDepartmentId(string departemenId)
        {
            var roles = _context.GroupRoles.Where(gr => gr.DepartemenId == departemenId).ToList();
            if (roles.Any())
            {
                _context.GroupRoles.RemoveRange(roles);
                _context.SaveChanges();
            }
        }
    }
}
