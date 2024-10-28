﻿using Microsoft.EntityFrameworkCore;
using PurchasingSystemStaging.Areas.Order.Models;
using PurchasingSystemStaging.Areas.Transaction.Models;
using PurchasingSystemStaging.Data;

namespace PurchasingSystemStaging.Areas.Transaction.Repositories
{
    public class IUnitRequestRepository
    {
        private string _errors = "";
        private readonly ApplicationDbContext _context;

        public IUnitRequestRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public string GetErrors()
        {
            return _errors;
        }

        public UnitRequest Tambah(UnitRequest UnitRequest)
        {
            _context.UnitRequests.Add(UnitRequest);
            _context.SaveChanges();
            return UnitRequest;
        }

        public async Task<UnitRequest> GetUnitRequestById(Guid Id)
        {
            var UnitRequest = _context.UnitRequests
                .Where(i => i.UnitRequestId == Id)
                .Include(d => d.UnitRequestDetails)
                .Include(u => u.ApplicationUser)
                .Include(z => z.UnitLocation)
                .Include(a => a.UnitRequestManager)
                .Include(t => t.WarehouseLocation)
                .Include(w => w.WarehouseApproval)
                .FirstOrDefault(p => p.UnitRequestId == Id);

            if (UnitRequest != null)
            {
                var UnitRequestDetail = new UnitRequest()
                {
                    UnitRequestId = UnitRequest.UnitRequestId,
                    UnitRequestNumber = UnitRequest.UnitRequestNumber,
                    UserAccessId = UnitRequest.UserAccessId,
                    ApplicationUser = UnitRequest.ApplicationUser,
                    UnitLocationId = UnitRequest.UnitLocationId,
                    UnitLocation = UnitRequest.UnitLocation,
                    UnitRequestManagerId = UnitRequest.UnitRequestManagerId,
                    UnitRequestManager = UnitRequest.UnitRequestManager,
                    WarehouseLocationId = UnitRequest.WarehouseLocationId,
                    WarehouseLocation = UnitRequest.WarehouseLocation,
                    WarehouseApprovalId = UnitRequest.WarehouseApprovalId,
                    WarehouseApproval = UnitRequest.WarehouseApproval,
                    QtyTotal = UnitRequest.QtyTotal,
                    Status = UnitRequest.Status,
                    Note = UnitRequest.Note,
                    UnitRequestDetails = UnitRequest.UnitRequestDetails,
                };
                return UnitRequestDetail;
            }
            return UnitRequest;
        }

        public async Task<UnitRequest> GetUnitRequestByIdNoTracking(Guid Id)
        {
            return await _context.UnitRequests.AsNoTracking().Where(i => i.UnitRequestId == Id).FirstOrDefaultAsync(a => a.UnitRequestId == Id);
        }

        public async Task<List<UnitRequest>> GetUnitRequests()
        {
            return await _context.UnitRequests./*OrderBy(p => p.CreateDateTime).*/Select(UnitRequest => new UnitRequest()
            {
                UnitRequestId = UnitRequest.UnitRequestId,
                UnitRequestNumber = UnitRequest.UnitRequestNumber,
                UserAccessId = UnitRequest.UserAccessId,
                ApplicationUser = UnitRequest.ApplicationUser,
                UnitLocationId = UnitRequest.UnitLocationId,
                UnitLocation = UnitRequest.UnitLocation,
                UnitRequestManagerId = UnitRequest.UnitRequestManagerId,
                UnitRequestManager = UnitRequest.UnitRequestManager,
                WarehouseLocationId = UnitRequest.WarehouseLocationId,
                WarehouseLocation = UnitRequest.WarehouseLocation,
                WarehouseApprovalId = UnitRequest.WarehouseApprovalId,
                WarehouseApproval = UnitRequest.WarehouseApproval,
                QtyTotal = UnitRequest.QtyTotal,
                Status = UnitRequest.Status,
                Note = UnitRequest.Note,
                UnitRequestDetails = UnitRequest.UnitRequestDetails,
            }).ToListAsync();
        }

        public IEnumerable<UnitRequest> GetAllUnitRequest()
        {
            return _context.UnitRequests.OrderByDescending(c => c.CreateDateTime)
                .Include(d => d.UnitRequestDetails)
                .Include(u => u.ApplicationUser)
                .Include(z => z.UnitLocation)
                .Include(a => a.UnitRequestManager)
                .Include(t => t.WarehouseLocation)
                .Include(w => w.WarehouseApproval)
                .ToList();
        }

        public IEnumerable<UnitRequest> GetAllUnitRequestAsc()
        {
            return _context.UnitRequests
                .Include(d => d.UnitRequestDetails)
                .Include(u => u.ApplicationUser)
                .Include(z => z.UnitLocation)
                .Include(a => a.UnitRequestManager)
                .Include(t => t.WarehouseLocation)
                .Include(w => w.WarehouseApproval)
                .ToList();
        }

        public async Task<UnitRequest> Update(UnitRequest update)
        {
            List<UnitRequestDetail> UnitRequestDetails = _context.UnitRequestDetails.Where(d => d.UnitRequestId == update.UnitRequestId).ToList();
            _context.UnitRequestDetails.RemoveRange(UnitRequestDetails);
            _context.SaveChanges();

            var UnitRequest = _context.UnitRequests.Attach(update);
            UnitRequest.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            _context.UnitRequestDetails.AddRangeAsync(update.UnitRequestDetails);
            _context.SaveChanges();
            return update;
        }

        public UnitRequest Delete(Guid Id)
        {
            var UnitRequest = _context.UnitRequests.Find(Id);
            if (UnitRequest != null)
            {
                _context.UnitRequests.Remove(UnitRequest);
                _context.SaveChanges();
            }
            return UnitRequest;
        }
    }
}
