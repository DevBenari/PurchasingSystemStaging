﻿using Microsoft.EntityFrameworkCore;
using PurchasingSystem.Areas.MasterData.Models;
using PurchasingSystem.Data;
using PurchasingSystem.Models;
using PurchasingSystem.Repositories;

namespace PurchasingSystem.Areas.MasterData.Repositories
{
    public class IUserActiveRepository
    {
        private readonly ApplicationDbContext _context;

        public IUserActiveRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public UserActive Tambah(UserActive user)
        {
            _context.UserActives.Add(user);
            _context.SaveChanges();
            return user;
        }

        public async Task<UserActive> GetUserById(Guid Id)
        {
            var user = await _context.UserActives
                .Include(p => p.Department)
                .Include(c => c.Position)
                .SingleOrDefaultAsync(i => i.UserActiveId == Id);

            if (user != null)
            {
                var userDetail = new UserActive()
                {
                    UserActiveId = user.UserActiveId,
                    UserActiveCode = user.UserActiveCode,
                    FullName = user.FullName,
                    IdentityNumber = user.IdentityNumber,
                    DepartmentId = user.DepartmentId,
                    PositionId = user.PositionId,
                    PlaceOfBirth = user.PlaceOfBirth,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    Address = user.Address,
                    Handphone = user.Handphone,
                    Email = user.Email,
                    Foto = user.Foto,
                    IsActive = user.IsActive
                };
                return userDetail;
            }
            return null;
        }

        public async Task<UserActive> GetUserByIdNoTracking(Guid Id)
        {
            return await _context.UserActives.AsNoTracking().FirstOrDefaultAsync(a => a.UserActiveId == Id);
        }

        public async Task<List<UserActive>> GetUserActives()
        {
            return await _context.UserActives.OrderBy(p => p.CreateDateTime).Where(u => u.FullName != "Administrator").Select(user => new UserActive()
            {
                UserActiveId = user.UserActiveId,
                UserActiveCode = user.UserActiveCode,
                FullName = user.FullName,
                IdentityNumber = user.IdentityNumber,
                DepartmentId = user.DepartmentId,
                PositionId = user.PositionId,
                PlaceOfBirth = user.PlaceOfBirth,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                Address = user.Address,
                Handphone = user.Handphone,
                Email = user.Email,
                Foto = user.Foto,
                IsActive = user.IsActive
            }).ToListAsync();
        }

        public IEnumerable<UserActive> GetAllUser()
        {
            return _context.UserActives.OrderByDescending(d => d.CreateDateTime)
                .Include(p => p.Department)
                .Include(c => c.Position)
                .AsNoTracking();
        }

        public IEnumerable<ApplicationUser> GetAllUserLogin()
        {
            return _context.Users
                .AsNoTracking();
        }

        public async Task<(IEnumerable<UserActive> userActives, int totalCountUserActives)> GetAllUserActivePageSize(string searchTerm, int page, int pageSize, DateTimeOffset? startDate, DateTimeOffset? endDate)
        {
            var query = _context.UserActives
                .OrderByDescending(d => d.CreateDateTime)
                .Include(p => p.Department)
                .Include(c => c.Position)
                .AsQueryable();

            // Filter berdasarkan searchTerm jika ada
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.FullName.Contains(searchTerm) || p.Email.Contains(searchTerm));
            }

            if (startDate.HasValue)
            {
                query = query.Where(p => p.CreateDateTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(p => p.CreateDateTime <= endDate.Value);
            }

            var totalCount = await query.CountAsync();

            // Ambil data paginated
            var userActives = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (userActives, totalCount);
        }

        public UserActive Update(UserActive update)
        {
            var user = _context.UserActives.Attach(update);
            user.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            _context.SaveChanges();
            return update;
        }

        public UserActive Delete(Guid Id)
        {
            var user = _context.UserActives.Find(Id);
            if (user != null)
            {
                _context.UserActives.Remove(user);
                _context.SaveChanges();
            }
            return user;
        }
    }
}
