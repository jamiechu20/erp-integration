using ErpSync.Application.Interfaces;
using ErpSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpSync.Infrastructure.Persistence;

public class ErpDbContext : DbContext, IUnitOfWork
{
    public ErpDbContext(DbContextOptions<ErpDbContext> options) : base(options)
    {
    }

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();
    public DbSet<Anomaly> Anomalies => Set<Anomaly>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.PurchaseOrderNumber);
            entity.Property(e => e.PurchaseOrderNumber).HasMaxLength(10);

            entity.HasMany(e => e.Items)
                .WithOne()
                .HasForeignKey(i => i.PurchaseOrder)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.HasKey(e => new { e.PurchaseOrder, e.PurchaseOrderItemNumber });
            entity.Property(e => e.PurchaseOrder).HasMaxLength(10);
            entity.Property(e => e.PurchaseOrderItemNumber).HasMaxLength(5);
            entity.Property(e => e.OrderQuantity).HasPrecision(18, 3);
            entity.Property(e => e.NetPriceAmount).HasPrecision(18, 2);
            entity.Property(e => e.NetAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<SyncLog>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<Anomaly>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RuleType).HasConversion<string>().HasMaxLength(20);
        });
    }
}
