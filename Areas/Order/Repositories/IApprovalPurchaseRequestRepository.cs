﻿using Microsoft.EntityFrameworkCore;
using PurchasingSystem.Areas.MasterData.Models;
using PurchasingSystem.Areas.Order.Models;
using PurchasingSystem.Data;

namespace PurchasingSystem.Areas.Order.Repositories
{
    public class IApprovalPurchaseRequestRepository
    {
        private string _errors = "";
        private readonly ApplicationDbContext _context;

        public IApprovalPurchaseRequestRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public string GetErrors()
        {
            return _errors;
        }

        public ApprovalPurchaseRequest Tambah(ApprovalPurchaseRequest Approval)
        {
            _context.Approvals.Add(Approval);
            _context.SaveChanges();
            return Approval;
        }

        public async Task<ApprovalPurchaseRequest> GetApprovalById(Guid Id)
        {
            var Approval = _context.Approvals
                .Where(i => i.ApprovalId == Id)
                .Include(u => u.ApplicationUser)
                .Include(t => t.PurchaseRequest)
                .Include(a1 => a1.UserApprove)
                .FirstOrDefault(p => p.ApprovalId == Id);

            if (Approval != null)
            {
                var ApprovalDetail = new ApprovalPurchaseRequest()
                {
                    CreateDateTime = Approval.CreateDateTime,
                    CreateBy = Approval.CreateBy,
                    UpdateDateTime = Approval.UpdateDateTime,
                    UpdateBy = Approval.UpdateBy,
                    DeleteDateTime = Approval.DeleteDateTime,
                    DeleteBy = Approval.DeleteBy,
                    ApprovalId = Approval.ApprovalId,
                    PurchaseRequestId = Approval.PurchaseRequestId,
                    PurchaseRequestNumber = Approval.PurchaseRequestNumber,
                    UserAccessId = Approval.UserAccessId,
                    ApplicationUser = Approval.ApplicationUser,
                    ExpiredDay = Approval.ExpiredDay,
                    RemainingDay = Approval.RemainingDay,
                    ExpiredDate = Approval.ExpiredDate,
                    UserApproveId = Approval.UserApproveId,
                    ApproveBy = Approval.ApproveBy,
                    ApprovalTime = Approval.ApprovalTime,
                    ApprovalDate = Approval.ApprovalDate,
                    ApprovalStatusUser = Approval.ApprovalStatusUser,
                    Status = Approval.Status,
                    Message = Approval.Message,
                    Note = Approval.Note
                };
                return ApprovalDetail;
            }
            return Approval;
        }

        public async Task<ApprovalPurchaseRequest> GetApprovalByIdNoTracking(Guid Id)
        {
            return await _context.Approvals.AsNoTracking().FirstOrDefaultAsync(a => a.ApprovalId == Id);
        }

        public async Task<List<ApprovalPurchaseRequest>> GetApprovals()
        {
            return await _context.Approvals./*OrderBy(p => p.CreateDateTime).*/Select(Approval => new ApprovalPurchaseRequest()
            {
                CreateDateTime = Approval.CreateDateTime,
                CreateBy = Approval.CreateBy,
                UpdateDateTime = Approval.UpdateDateTime,
                UpdateBy = Approval.UpdateBy,
                DeleteDateTime = Approval.DeleteDateTime,
                DeleteBy = Approval.DeleteBy,
                ApprovalId = Approval.ApprovalId,
                PurchaseRequestId = Approval.PurchaseRequestId,
                PurchaseRequestNumber = Approval.PurchaseRequestNumber,
                UserAccessId = Approval.UserAccessId,
                ApplicationUser = Approval.ApplicationUser,
                ExpiredDay = Approval.ExpiredDay,
                RemainingDay = Approval.RemainingDay,
                ExpiredDate = Approval.ExpiredDate,
                UserApproveId = Approval.UserApproveId,
                ApproveBy = Approval.ApproveBy,
                ApprovalTime = Approval.ApprovalTime,
                ApprovalDate = Approval.ApprovalDate,
                ApprovalStatusUser = Approval.ApprovalStatusUser,
                Status = Approval.Status,
                Message = Approval.Message,
                Note = Approval.Note
            }).ToListAsync();
        }

        public IEnumerable<ApprovalPurchaseRequest> GetAllApproval()
        {
            return _context.Approvals.OrderByDescending(c => c.CreateDateTime)
                .Include(u => u.ApplicationUser)
                .Include(t => t.PurchaseRequest)
                .Include(a1 => a1.UserApprove)
                .ToList();
        }

        public IEnumerable<ApprovalPurchaseRequest> GetAllApprovalById(Guid Id)
        {
            return _context.Approvals
                .Where(i => i.UserApproveId == Id)
                .OrderByDescending(c => c.CreateDateTime)
                .Include(u => u.ApplicationUser)
                .Include(t => t.PurchaseRequest)
                .Include(a1 => a1.UserApprove)
                .ToList();
        }

        public async Task<(IEnumerable<ApprovalPurchaseRequest> approvals, int totalCountApprovals)> GetAllApprovalPageSize(string searchTerm, int page, int pageSize, DateTimeOffset? startDate, DateTimeOffset? endDate)
        {
            var query = _context.Approvals
                .Include(u => u.ApplicationUser)
                .Include(t => t.PurchaseRequest)
                .Include(a1 => a1.UserApprove)
                .OrderByDescending(d => d.CreateDateTime)
                .AsQueryable();

            // Filter berdasarkan searchTerm jika ada
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.PurchaseRequestNumber.Contains(searchTerm));
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
            var approvals = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (approvals, totalCount);
        }

        public IEnumerable<ApprovalPurchaseRequest> GetChartBeforeExpired(Guid Id)
        {
            return _context.Approvals
                .Where(i => i.UserApproveId == Id && i.RemainingDay > 0)
                .Include(u => u.ApplicationUser)
                .Include(t => t.PurchaseRequest)
                .Include(a1 => a1.UserApprove)
                .ToList();
        }

        public IEnumerable<ApprovalPurchaseRequest> GetChartOnExpired(Guid Id)
        {
            return _context.Approvals
                .Where(i => i.UserApproveId == Id && i.RemainingDay == 0)
                .Include(u => u.ApplicationUser)
                .Include(t => t.PurchaseRequest)
                .Include(a1 => a1.UserApprove)
                .ToList();
        }

        public IEnumerable<ApprovalPurchaseRequest> GetChartMoreThanExpired(Guid Id)
        {
            return _context.Approvals
                .Where(i => i.UserApproveId == Id && i.RemainingDay < 0)
                .Include(u => u.ApplicationUser)
                .Include(t => t.PurchaseRequest)
                .Include(a1 => a1.UserApprove)
                .ToList();
        }

        public async Task<ApprovalPurchaseRequest> Update(ApprovalPurchaseRequest update)
        {          
            var approval = _context.Approvals.Attach(update);
            approval.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            _context.SaveChanges();
            return update;
        }

        public ApprovalPurchaseRequest Delete(Guid Id)
        {
            var Approval = _context.Approvals.Find(Id);
            if (Approval != null)
            {
                _context.Approvals.Remove(Approval);
                _context.SaveChanges();
            }
            return Approval;
        }
    }
}
