using Microsoft.EntityFrameworkCore;
using PurchasingSystemApps.Areas.MasterData.Models;
using PurchasingSystemApps.Data;

namespace PurchasingSystemApps.Areas.MasterData.Repositories
{
    public class IDueDateRepository
    {
        private readonly ApplicationDbContext _context;

        public IDueDateRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public DueDate Tambah(DueDate DueDate)
        {
            _context.DueDates.Add(DueDate);
            _context.SaveChanges();
            return DueDate;
        }

        public async Task<DueDate> GetDueDateById(Guid Id)
        {
            var DueDate = await _context.DueDates
                .SingleOrDefaultAsync(i => i.DueDateId == Id);

            if (DueDate != null)
            {
                var DueDateDetail = new DueDate()
                {
                    DueDateId = DueDate.DueDateId,
                    Value = DueDate.Value
                };
                return DueDateDetail;
            }
            return null;
        }

        public async Task<DueDate> GetDueDateByIdNoTracking(Guid Id)
        {
            return await _context.DueDates.AsNoTracking().FirstOrDefaultAsync(a => a.DueDateId == Id);
        }

        public async Task<List<DueDate>> GetDueDates()
        {
            return await _context.DueDates.OrderBy(p => p.CreateDateTime).Select(DueDate => new DueDate()
            {
                DueDateId = DueDate.DueDateId,
                Value = DueDate.Value
            }).ToListAsync();
        }

        public IEnumerable<DueDate> GetAllDueDate()
        {
            return _context.DueDates.OrderByDescending(d => d.CreateDateTime)
                .AsNoTracking();
        }

        public DueDate Update(DueDate update)
        {
            var DueDate = _context.DueDates.Attach(update);
            DueDate.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            _context.SaveChanges();
            return update;
        }

        public DueDate Delete(Guid Id)
        {
            var DueDate = _context.DueDates.Find(Id);
            if (DueDate != null)
            {
                _context.DueDates.Remove(DueDate);
                _context.SaveChanges();
            }
            return DueDate;
        }
    }
}
