using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sgo.Domain.Catalog;
using Sgo.Domain.Organization;
using Sgo.Domain.Purchasing;

namespace Sgo.Infrastructure.Persistence.Configurations.Purchasing;

internal sealed class PurchaseRequisitionConfiguration : IEntityTypeConfiguration<PurchaseRequisition>
{
    public void Configure(EntityTypeBuilder<PurchaseRequisition> builder)
    {
        builder.ToTable("purchase_requisition", "purchasing");
        builder.Property(r => r.Folio).HasMaxLength(30);
        builder.Property(r => r.Status).HasMaxLength(30);
        builder.Property(r => r.Notes).HasMaxLength(500);
        builder.Property(r => r.RejectionReason).HasMaxLength(500);
        builder.HasIndex(r => r.Folio).IsUnique();
        builder.HasIndex(r => new { r.LocationId, r.Status });
        builder.HasIndex(r => r.Status);
        builder.HasOne<Location>().WithMany().HasForeignKey(r => r.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(r => r.Lines).WithOne().HasForeignKey(l => l.RequisitionId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PurchaseRequisitionLineConfiguration : IEntityTypeConfiguration<PurchaseRequisitionLine>
{
    public void Configure(EntityTypeBuilder<PurchaseRequisitionLine> builder)
    {
        builder.ToTable("purchase_requisition_line", "purchasing");
        builder.HasIndex(l => l.ItemId);
        builder.HasIndex(l => l.SuggestedSupplierId);
        builder.HasOne<Item>().WithMany().HasForeignKey(l => l.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Supplier>().WithMany().HasForeignKey(l => l.SuggestedSupplierId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("purchase_order", "purchasing");
        builder.Property(o => o.Folio).HasMaxLength(30);
        builder.Property(o => o.Status).HasMaxLength(30);
        builder.Property(o => o.Notes).HasMaxLength(500);
        builder.Property(o => o.Subtotal).HasPrecision(18, 2);
        builder.Property(o => o.TaxTotal).HasPrecision(18, 2);
        builder.Property(o => o.Total).HasPrecision(18, 2);
        builder.HasIndex(o => o.Folio).IsUnique();
        builder.HasIndex(o => new { o.DeliveryLocationId, o.Status });
        builder.HasIndex(o => new { o.SupplierId, o.Status });
        builder.HasIndex(o => o.Status);
        builder.HasOne<Supplier>().WithMany().HasForeignKey(o => o.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Location>().WithMany().HasForeignKey(o => o.DeliveryLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(o => o.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("purchase_order_line", "purchasing");
        builder.Property(l => l.TaxRate).HasPrecision(5, 4);
        builder.Ignore(l => l.Subtotal);
        builder.Ignore(l => l.TaxAmount);
        builder.HasIndex(l => l.ItemId);
        builder.HasIndex(l => l.RequisitionLineId);
        builder.HasOne<Item>().WithMany().HasForeignKey(l => l.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PurchaseRequisitionLine>().WithMany().HasForeignKey(l => l.RequisitionLineId).OnDelete(DeleteBehavior.Restrict);
    }
}
