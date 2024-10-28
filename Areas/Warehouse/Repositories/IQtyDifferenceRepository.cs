﻿using Microsoft.EntityFrameworkCore;
using PurchasingSystemStaging.Areas.Warehouse.Models;
using PurchasingSystemStaging.Data;

namespace PurchasingSystemStaging.Areas.Warehouse.Repositories
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
                .Include(d1 => d1.Department1)
                .Include(p1 => p1.Position1)
                .Include(a1 => a1.UserApprove1)
                .Include(d2 => d2.Department2)
                .Include(p2 => p2.Position2)
                .Include(a2 => a2.UserApprove2)
                .Include(u => u.ApplicationUser)
                .FirstOrDefault(p => p.QtyDifferenceId == Id);

            if (QtyDifference != null)
            {
                var QtyDifferenceDetail = new QtyDifference()
                {
                    QtyDifferenceId = QtyDifference.QtyDifferenceId,
                    QtyDifferenceNumber = QtyDifference.QtyDifferenceNumber,
                    PurchaseOrderId = QtyDifference.PurchaseOrderId,
                    PurchaseOrder = QtyDifference.PurchaseOrder,
                    PurchaseOrderNumber = QtyDifference.PurchaseOrderNumber,
                    UserAccessId = QtyDifference.UserAccessId,
                    Department1Id = QtyDifference.Department1Id,
                    Department1 = QtyDifference.Department1,
                    Position1Id = QtyDifference.Position1Id,
                    Position1 = QtyDifference.Position1,
                    UserApprove1Id = QtyDifference.UserApprove1Id,
                    UserApprove1 = QtyDifference.UserApprove1,
                    ApproveStatusUser1 = QtyDifference.ApproveStatusUser1,                    
                    Department2Id = QtyDifference.Department2Id,
                    Department2 = QtyDifference.Department2,
                    Position2Id = QtyDifference.Position2Id,
                    Position2 = QtyDifference.Position2,
                    UserApprove2Id = QtyDifference.UserApprove2Id,
                    UserApprove2 = QtyDifference.UserApprove2,
                    ApproveStatusUser2 = QtyDifference.ApproveStatusUser2,
                    Status = QtyDifference.Status,
                    Note = QtyDifference.Note,
                    MessageApprove1 = QtyDifference.MessageApprove1,
                    MessageApprove2 = QtyDifference.MessageApprove2,
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
                PurchaseOrder = QtyDifference.PurchaseOrder,
                PurchaseOrderNumber = QtyDifference.PurchaseOrderNumber,
                UserAccessId = QtyDifference.UserAccessId,
                Department1Id = QtyDifference.Department1Id,
                Department1 = QtyDifference.Department1,
                Position1Id = QtyDifference.Position1Id,
                Position1 = QtyDifference.Position1,
                UserApprove1Id = QtyDifference.UserApprove1Id,
                UserApprove1 = QtyDifference.UserApprove1,
                ApproveStatusUser1 = QtyDifference.ApproveStatusUser1,
                Department2Id = QtyDifference.Department2Id,
                Department2 = QtyDifference.Department2,
                Position2Id = QtyDifference.Position2Id,
                Position2 = QtyDifference.Position2,
                UserApprove2Id = QtyDifference.UserApprove2Id,
                UserApprove2 = QtyDifference.UserApprove2,
                ApproveStatusUser2 = QtyDifference.ApproveStatusUser2,
                Status = QtyDifference.Status,
                Note = QtyDifference.Note,
                MessageApprove1 = QtyDifference.MessageApprove1,
                MessageApprove2 = QtyDifference.MessageApprove2,
                QtyDifferenceDetails = QtyDifference.QtyDifferenceDetails
            }).ToListAsync();
        }

        public IEnumerable<QtyDifference> GetAllQtyDifference()
        {
            return _context.QtyDifferences.OrderByDescending(c => c.CreateDateTime)
                .Include(d => d.PurchaseOrder)
                .Include(a => a.PurchaseOrderDetails)
                .Include(r => r.QtyDifferenceDetails)
                .Include(d1 => d1.Department1)
                .Include(p1 => p1.Position1)
                .Include(a1 => a1.UserApprove1)
                .Include(d2 => d2.Department2)
                .Include(p2 => p2.Position2)
                .Include(a2 => a2.UserApprove2)
                .Include(u => u.ApplicationUser)
                .ToList();
        }
    }
}
