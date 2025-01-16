using Microsoft.EntityFrameworkCore;
using PurchasingSystem.Areas.MasterData.Models;
using PurchasingSystem.Areas.Order.Models;
using PurchasingSystem.Data;

namespace PurchasingSystem.Areas.Order.Repositories
{
    public class IEmailRepository
    {
        private readonly ApplicationDbContext _context;
        public IEmailRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Email> GetAllEmails()
        {
            return _context.Emails.AsNoTracking();
        }

        public async Task<(IEnumerable<Email> emails, int totalCountEmails)> GetAllEmailPageSize(string searchTerm, int page, int pageSize, DateTimeOffset? startDate, DateTimeOffset? endDate)
        {
            var query = _context.Emails
                .OrderByDescending(d => d.CreateDateTime)
                .AsQueryable();

            // Filter berdasarkan searchTerm jika ada
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.To.Contains(searchTerm) || p.Subject.Contains(searchTerm));
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
            var emails = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (emails, totalCount);
        }

        public Email Tambah(Email email)
        {
            _context.Emails.Add(email);
            _context.SaveChanges();
            return email;
        }

    }
}
