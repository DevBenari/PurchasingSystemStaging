﻿using Microsoft.EntityFrameworkCore;
using PurchasingSystemStaging.Areas.Warehouse.Models;
using PurchasingSystemStaging.Data;

namespace PurchasingSystemStaging.Areas.Warehouse.Repositories
{
    public class IReceiveOrderRepository
    {
        private string _errors = "";
        private readonly ApplicationDbContext _context;

        public IReceiveOrderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public string GetErrors()
        {
            return _errors;
        }

        public ReceiveOrder Tambah(ReceiveOrder ReceiveOrder)
        {
            _context.ReceiveOrders.Add(ReceiveOrder);
            _context.SaveChanges();
            return ReceiveOrder;
        }

        public async Task<ReceiveOrder> GetReceiveOrderById(Guid Id)
        {
            var receiveOrder = _context.ReceiveOrders
                .Where(i => i.ReceiveOrderId == Id)
                .Include(d => d.PurchaseOrder)
                .Include(d => d.PurchaseOrderDetails)
                .Include(r => r.ReceiveOrderDetails)
                .Include(u => u.ApplicationUser)
                .FirstOrDefault(p => p.ReceiveOrderId == Id);

            if (receiveOrder != null)
            {
                var receiveOrderDetail = new ReceiveOrder()
                {
                    CreateDateTime = receiveOrder.CreateDateTime,
                    CreateBy = receiveOrder.CreateBy,
                    UpdateDateTime = receiveOrder.UpdateDateTime,
                    UpdateBy = receiveOrder.UpdateBy,
                    DeleteDateTime = receiveOrder.DeleteDateTime,
                    DeleteBy = receiveOrder.DeleteBy,
                    ReceiveOrderId = receiveOrder.ReceiveOrderId,
                    ReceiveOrderNumber = receiveOrder.ReceiveOrderNumber,
                    PurchaseOrderId = receiveOrder.PurchaseOrderId,
                    ReceiveById = receiveOrder.ReceiveById,
                    Status = receiveOrder.Status,
                    Note = receiveOrder.Note,
                    ReceiveOrderDetails = receiveOrder.ReceiveOrderDetails
                };
                return receiveOrderDetail;
            }
            return receiveOrder;
        }

        public async Task<ReceiveOrder> GetReceiveOrderByIdNoTracking(Guid Id)
        {
            return await _context.ReceiveOrders.AsNoTracking().Where(i => i.ReceiveOrderId == Id).FirstOrDefaultAsync(a => a.ReceiveOrderId == Id);
        }

        public async Task<List<ReceiveOrder>> GetReceiveOrders()
        {
            return await _context.ReceiveOrders.OrderBy(p => p.CreateDateTime).Select(receiveOrder => new ReceiveOrder()
            {
                CreateDateTime = receiveOrder.CreateDateTime,
                CreateBy = receiveOrder.CreateBy,
                UpdateDateTime = receiveOrder.UpdateDateTime,
                UpdateBy = receiveOrder.UpdateBy,
                DeleteDateTime = receiveOrder.DeleteDateTime,
                DeleteBy = receiveOrder.DeleteBy,
                ReceiveOrderId = receiveOrder.ReceiveOrderId,
                ReceiveOrderNumber = receiveOrder.ReceiveOrderNumber,
                PurchaseOrderId = receiveOrder.PurchaseOrderId,
                ReceiveById = receiveOrder.ReceiveById,
                Status = receiveOrder.Status,
                Note = receiveOrder.Note,
                ReceiveOrderDetails = receiveOrder.ReceiveOrderDetails
            }).ToListAsync();
        }

        public IEnumerable<ReceiveOrder> GetAllReceiveOrder()
        {
            return _context.ReceiveOrders.OrderByDescending(c => c.CreateDateTime)
                .Include(d => d.PurchaseOrder)
                .Include(d => d.PurchaseOrderDetails)
                .Include(r => r.ReceiveOrderDetails)
                .Include(u => u.ApplicationUser)
                .ToList();
        }
    }
}
