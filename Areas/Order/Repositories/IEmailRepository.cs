using Microsoft.EntityFrameworkCore;
using PurchasingSystemApps.Areas.MasterData.Models;
using PurchasingSystemApps.Areas.Order.Models;
using PurchasingSystemApps.Data;

namespace PurchasingSystemApps.Areas.Order.Repositories
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

        public Email Tambah(Email email)
        {
            _context.Emails.Add(email);
            _context.SaveChanges();
            return email;
        }

    }
}
