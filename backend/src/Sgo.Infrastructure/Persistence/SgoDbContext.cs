using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sgo.Application.Common;
using Sgo.Domain.Catalog;
using Sgo.Domain.Common;
using Sgo.Domain.Inventory;
using Sgo.Domain.Organization;
using Sgo.Domain.Security;
using Sgo.Infrastructure.Identity;

namespace Sgo.Infrastructure.Persistence;

public class SgoDbContext(DbContextOptions<SgoDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options), ISgoDbContext
{
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemLocationSetting> ItemLocationSettings => Set<ItemLocationSetting>();
    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<ItemLocationCost> ItemLocationCosts => Set<ItemLocationCost>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<InventoryAdjustment> InventoryAdjustments => Set<InventoryAdjustment>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserLocation> UserLocations => Set<UserLocation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Quantities and costs; document amounts override to (18,2) per property.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 4);
        configurationBuilder.Properties<Enum>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureIdentityTables(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(SgoDbContext).Assembly);

        foreach (var folio in Folio.Definitions.Values)
            builder.HasSequence<long>(folio.SequenceName, folio.Schema);

        foreach (var entityType in builder.Model.GetEntityTypes()
                     .Where(t => typeof(IVersioned).IsAssignableFrom(t.ClrType)))
        {
            builder.Entity(entityType.ClrType).Property(nameof(IVersioned.Version)).IsRowVersion();
        }
    }

    private static void ConfigureIdentityTables(ModelBuilder builder)
    {
        const string schema = "security";
        builder.Entity<AppUser>(e =>
        {
            e.ToTable("user", schema);
            e.Property(u => u.FullName).HasMaxLength(200);
        });
        builder.Entity<AppRole>(e =>
        {
            e.ToTable("role", schema);
            e.Property(r => r.Description).HasMaxLength(500);
            e.Property(r => r.SystemKey).HasMaxLength(50);
            e.HasIndex(r => r.SystemKey).IsUnique();
        });
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_role", schema);
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claim", schema);
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_login", schema);
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_token", schema);
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claim", schema);
    }
}
