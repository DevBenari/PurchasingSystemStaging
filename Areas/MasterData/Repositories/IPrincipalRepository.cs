using Microsoft.EntityFrameworkCore;
using PurchasingSystemApps.Areas.MasterData.Models;
using PurchasingSystemApps.Data;
using PurchasingSystemApps.Models;
using PurchasingSystemApps.Repositories;

namespace PurchasingSystemApps.Areas.MasterData.Repositories
{
    public class IPrincipalRepository
    {
        private readonly ApplicationDbContext _context;

        public IPrincipalRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Principal Tambah(Principal Principal)
        {
            _context.Principals.Add(Principal);
            _context.SaveChanges();
            return Principal;
        }

        public async Task<Principal> GetPrincipalById(Guid Id)
        {
            var Principal = await _context.Principals
                .SingleOrDefaultAsync(i => i.PrincipalId == Id);

            if (Principal != null)
            {
                var PrincipalDetail = new Principal()
                {
                    PrincipalId = Principal.PrincipalId,
                    PrincipalCode = Principal.PrincipalCode,
                    PrincipalName = Principal.PrincipalName,                   
                    Address = Principal.Address,
                    Handphone = Principal.Handphone,
                    Email = Principal.Email,
                    Note = Principal.Note
                };
                return PrincipalDetail;
            }
            return null;
        }

        public async Task<Principal> GetPrincipalByIdNoTracking(Guid Id)
        {
            return await _context.Principals.AsNoTracking().FirstOrDefaultAsync(a => a.PrincipalId == Id);
        }

        public async Task<List<Principal>> GetPrincipals()
        {
            return await _context.Principals.OrderBy(p => p.CreateDateTime).Select(Principal => new Principal()
            {
                PrincipalId = Principal.PrincipalId,
                PrincipalCode = Principal.PrincipalCode,
                PrincipalName = Principal.PrincipalName,
                Address = Principal.Address,
                Handphone = Principal.Handphone,
                Email = Principal.Email,
                Note = Principal.Note
            }).ToListAsync();
        }

        public IEnumerable<Principal> GetAllPrincipal()
        {
            return _context.Principals.OrderByDescending(d => d.CreateDateTime)
                .AsNoTracking();
        }

        public Principal Update(Principal update)
        {
            var Principal = _context.Principals.Attach(update);
            Principal.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            _context.SaveChanges();
            return update;
        }

        public Principal Delete(Guid Id)
        {
            var Principal = _context.Principals.Find(Id);
            if (Principal != null)
            {
                _context.Principals.Remove(Principal);
                _context.SaveChanges();
            }
            return Principal;
        }
    }
}
