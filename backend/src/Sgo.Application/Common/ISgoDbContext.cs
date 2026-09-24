using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Sgo.Domain.Catalog;
using Sgo.Domain.Organization;
using Sgo.Domain.Security;

namespace Sgo.Application.Common;

/// <summary>
/// The EF Core context as seen by application services (spec §3: services use the context directly,
/// without generic repositories). Implemented by <c>SgoDbContext</c> in Infrastructure.
/// </summary>
public interface ISgoDbContext
{
    DbSet<Location> Locations { get; }
    DbSet<AppSetting> AppSettings { get; }
    DbSet<UnitOfMeasure> UnitsOfMeasure { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserLocation> UserLocations { get; }
    DbSet<AuditLog> AuditLogs { get; }

    DatabaseFacade Database { get; }

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
