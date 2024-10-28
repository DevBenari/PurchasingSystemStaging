﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PurchasingSystemStaging.Areas.MasterData.Models;
using PurchasingSystemStaging.Areas.Order.Models;
using PurchasingSystemStaging.Areas.Transaction.Models;
using PurchasingSystemStaging.Areas.Warehouse.Models;
using PurchasingSystemStaging.Models;

namespace PurchasingSystemStaging.Data;
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : base(options)
    {
    }

    #region Areas Master Data
    public DbSet<UserActive> UserActives { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Discount> Discounts { get; set; }
    public DbSet<Measurement> Measurements { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<TermOfPayment> TermOfPayments { get; set; }
    public DbSet<Bank> Banks { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<LeadTime> LeadTimes { get; set; }
    public DbSet<InitialStock> InitialStocks { get; set; }
    public DbSet<WarehouseLocation> WarehouseLocations { get; set; }
    public DbSet<UnitLocation> UnitLocations { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Position> Positions { get; set; }
    #endregion

    #region Areas Order
    public DbSet<PurchaseRequest> PurchaseRequests { get; set; }
    public DbSet<PurchaseRequestDetail> PurchaseRequestDetails { get; set; }
    public DbSet<Approval> Approvals { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }
    public DbSet<QtyDifferenceRequest> QtyDifferenceRequests { get; set; }
    public DbSet<ApprovalQtyDifference> ApprovalQtyDifferences { get; set; }
    public DbSet<Email> Emails { get; set; }
    #endregion

    #region Areas Warehouse
    public DbSet<ReceiveOrder> ReceiveOrders { get; set; }
    public DbSet<WarehouseRequest> WarehouseRequests { get; set; }
    public DbSet<WarehouseRequestDetail> WarehouseRequestDetails { get; set; }
    public DbSet<WarehouseTransfer> WarehouseTransfers { get; set; }
    public DbSet<WarehouseTransferDetail> WarehouseTransferDetails { get; set; }
    public DbSet<QtyDifference> QtyDifferences { get; set; }
    public DbSet<QtyDifferenceDetail> QtyDifferenceDetails { get; set; }
    #endregion

    #region Areas Unit Request
    public DbSet<UnitRequest> UnitRequests { get; set; }
    public DbSet<UnitRequestDetail> UnitRequestDetails { get; set; }
    public DbSet<ApprovalRequest> ApprovalRequests { get; set; }
    #endregion

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);

        builder.ApplyConfiguration(new ApplicationUserEntityConfiguration());
        //builder.ApplyConfiguration(new ApplicationRoleEntityConfiguration());

        foreach (var foreignKey in builder.Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}

public class ApplicationUserEntityConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.KodeUser).HasMaxLength(255);
        builder.Property(u => u.NamaUser).HasMaxLength(255);
    }
}
