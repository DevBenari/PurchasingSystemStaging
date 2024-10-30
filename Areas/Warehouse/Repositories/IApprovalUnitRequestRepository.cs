﻿using Microsoft.EntityFrameworkCore;
using PurchasingSystemStaging.Areas.Warehouse.Models;
using PurchasingSystemStaging.Data;

namespace PurchasingSystemStaging.Areas.Warehouse.Repositories
{
    public class IApprovalUnitRequestRepository
    {
        private string _errors = "";
        private readonly ApplicationDbContext _context;

        public IApprovalUnitRequestRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public string GetErrors()
        {
            return _errors;
        }

        public ApprovalUnitRequest Tambah(ApprovalUnitRequest ApprovalRequest)
        {
            _context.ApprovalUnitRequests.Add(ApprovalRequest);
            _context.SaveChanges();
            return ApprovalRequest;
        }

        public async Task<ApprovalUnitRequest> GetApprovalRequestById(Guid Id)
        {
            var ApprovalRequest = _context.ApprovalUnitRequests
                .Where(i => i.ApprovalUnitRequestId == Id)
                .Include(d => d.UnitRequest)
                .Include(u => u.UnitLocation)
                .Include(a => a.WarehouseLocation)
                .Include(t => t.ApplicationUser)
                .Include(s => s.UserApprove)
                .FirstOrDefault(p => p.ApprovalUnitRequestId == Id);

            if (ApprovalRequest != null)
            {
                var ApprovalRequestDetail = new ApprovalUnitRequest()
                {
                    ApprovalUnitRequestId = ApprovalRequest.ApprovalUnitRequestId,
                    UnitRequestId = ApprovalRequest.UnitRequestId,
                    UnitRequestNumber = ApprovalRequest.UnitRequestNumber,
                    UserAccessId = ApprovalRequest.UserAccessId,
                    ApplicationUser = ApprovalRequest.ApplicationUser,
                    UnitLocationId = ApprovalRequest.UnitLocationId,
                    UnitLocation = ApprovalRequest.UnitLocation,
                    WarehouseLocationId = ApprovalRequest.WarehouseLocationId,
                    WarehouseLocation = ApprovalRequest.WarehouseLocation,
                    UserApproveId = ApprovalRequest.UserApproveId,
                    UserApprove = ApprovalRequest.UserApprove,
                    ApproveBy = ApprovalRequest.ApproveBy,
                    ApprovalTime = ApprovalRequest.ApprovalTime,
                    ApprovalDate = ApprovalRequest.ApprovalDate,
                    ApprovalStatusUser = ApprovalRequest.ApprovalStatusUser,
                    Status = ApprovalRequest.Status,
                    Note = ApprovalRequest.Note,
                    Message = ApprovalRequest.Message,
                };
                return ApprovalRequestDetail;
            }
            return ApprovalRequest;
        }

        public async Task<ApprovalUnitRequest> GetApprovalRequestByIdNoTracking(Guid Id)
        {
            return await _context.ApprovalUnitRequests.AsNoTracking().Where(i => i.ApprovalUnitRequestId == Id).FirstOrDefaultAsync(a => a.ApprovalUnitRequestId == Id);
        }

        public async Task<List<ApprovalUnitRequest>> GetApprovalUnitRequests()
        {
            return await _context.ApprovalUnitRequests./*OrderBy(p => p.CreateDateTime).*/Select(ApprovalRequest => new ApprovalUnitRequest()
            {
                ApprovalUnitRequestId = ApprovalRequest.ApprovalUnitRequestId,
                UnitRequestId = ApprovalRequest.UnitRequestId,
                UnitRequestNumber = ApprovalRequest.UnitRequestNumber,
                UserAccessId = ApprovalRequest.UserAccessId,
                ApplicationUser = ApprovalRequest.ApplicationUser,
                UnitLocationId = ApprovalRequest.UnitLocationId,
                UnitLocation = ApprovalRequest.UnitLocation,
                WarehouseLocationId = ApprovalRequest.WarehouseLocationId,
                WarehouseLocation = ApprovalRequest.WarehouseLocation,
                UserApproveId = ApprovalRequest.UserApproveId,
                UserApprove = ApprovalRequest.UserApprove,
                ApproveBy = ApprovalRequest.ApproveBy,
                ApprovalTime = ApprovalRequest.ApprovalTime,
                ApprovalDate = ApprovalRequest.ApprovalDate,
                ApprovalStatusUser = ApprovalRequest.ApprovalStatusUser,
                Status = ApprovalRequest.Status,
                Note = ApprovalRequest.Note,
                Message = ApprovalRequest.Message,
            }).ToListAsync();
        }

        public IEnumerable<ApprovalUnitRequest> GetAllApprovalRequest()
        {
            return _context.ApprovalUnitRequests.OrderByDescending(c => c.CreateDateTime)
                .Include(d => d.UnitRequest)
                .Include(u => u.UnitLocation)
                .Include(a => a.WarehouseLocation)
                .Include(t => t.ApplicationUser)
                .Include(s => s.UserApprove)
                .ToList();
        }

        public async Task<ApprovalUnitRequest> Update(ApprovalUnitRequest update)
        {
            var ApprovalRequest = _context.ApprovalUnitRequests.Attach(update);
            ApprovalRequest.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            _context.SaveChanges();
            return update;
        }

        public ApprovalUnitRequest Delete(Guid Id)
        {
            var ApprovalRequest = _context.ApprovalUnitRequests.Find(Id);
            if (ApprovalRequest != null)
            {
                _context.ApprovalUnitRequests.Remove(ApprovalRequest);
                _context.SaveChanges();
            }
            return ApprovalRequest;
        }
    }
}
