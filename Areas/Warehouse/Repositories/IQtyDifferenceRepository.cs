using Microsoft.EntityFrameworkCore;
using PurchasingSystemApps.Areas.Warehouse.Models;
using PurchasingSystemApps.Data;

namespace PurchasingSystemApps.Areas.Warehouse.Repositories
{
    public class IQtyDifferenceRepository
    {
        private string _errors = "";
        private readonly ApplicationDbContext _context;

        public IQtyDifferenceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public string GetErrors()
        {
            return _errors;
        }

        public QtyDifference Tambah(QtyDifference QtyDifference)
        {
            _context.QtyDifferences.Add(QtyDifference);
            _context.SaveChanges();
            return QtyDifference;
        }

        public async Task<QtyDifference> GetQtyDifferenceById(Guid Id)
        {
            var QtyDifference = _context.QtyDifferences
                .Where(i => i.QtyDifferenceId == Id)
                .Include(d => d.PurchaseOrder)
                .Include(a => a.PurchaseOrderDetails)
                .Include(r => r.QtyDifferenceDetails)
                .Include(u => u.ApplicationUser)
                .Include(s => s.HeadWarehouseManager)
                .Include(f => f.HeadPurchasingManager)
                .FirstOrDefault(p => p.QtyDifferenceId == Id);

            if (QtyDifference != null)
            {
                var QtyDifferenceDetail = new QtyDifference()
                {
                    QtyDifferenceId = QtyDifference.QtyDifferenceId,
                    QtyDifferenceNumber = QtyDifference.QtyDifferenceNumber,
                    PurchaseOrderId = QtyDifference.PurchaseOrderId,
                    PurchaseOrder = QtyDifference.PurchaseOrder,
                    CheckedById = QtyDifference.CheckedById,
                    ApplicationUser = QtyDifference.ApplicationUser,
                    HeadWarehouseManagerId = QtyDifference.HeadWarehouseManagerId,
                    HeadWarehouseManager = QtyDifference.HeadWarehouseManager,
                    HeadPurchasingManagerId = QtyDifference.HeadPurchasingManagerId,
                    HeadPurchasingManager = QtyDifference.HeadPurchasingManager,
                    Status = QtyDifference.Status,
                    Note = QtyDifference.Note,
                    QtyDifferenceDetails = QtyDifference.QtyDifferenceDetails
                };
                return QtyDifferenceDetail;
            }
            return QtyDifference;
        }

        public async Task<QtyDifference> GetQtyDifferenceByIdNoTracking(Guid Id)
        {
            return await _context.QtyDifferences.AsNoTracking().Where(i => i.QtyDifferenceId == Id).FirstOrDefaultAsync(a => a.QtyDifferenceId == Id);
        }

        public async Task<List<QtyDifference>> GetQtyDifferences()
        {
            return await _context.QtyDifferences.OrderBy(p => p.CreateDateTime).Select(QtyDifference => new QtyDifference()
            {
                QtyDifferenceId = QtyDifference.QtyDifferenceId,
                QtyDifferenceNumber = QtyDifference.QtyDifferenceNumber,
                PurchaseOrderId = QtyDifference.PurchaseOrderId,
                CheckedById = QtyDifference.CheckedById,
                HeadWarehouseManagerId = QtyDifference.HeadWarehouseManagerId,
                HeadWarehouseManager = QtyDifference.HeadWarehouseManager,
                HeadPurchasingManagerId = QtyDifference.HeadPurchasingManagerId,
                HeadPurchasingManager = QtyDifference.HeadPurchasingManager,
                Status = QtyDifference.Status,
                Note = QtyDifference.Note,
                QtyDifferenceDetails = QtyDifference.QtyDifferenceDetails
            }).ToListAsync();
        }

        public IEnumerable<QtyDifference> GetAllQtyDifference()
        {
            return _context.QtyDifferences.OrderByDescending(c => c.CreateDateTime)
                .Include(d => d.PurchaseOrder)
                .Include(a => a.PurchaseOrderDetails)
                .Include(r => r.QtyDifferenceDetails)
                .Include(u => u.ApplicationUser)
                .Include(s => s.HeadWarehouseManager)
                .Include(f => f.HeadPurchasingManager)
                .ToList();
        }
    }
}
