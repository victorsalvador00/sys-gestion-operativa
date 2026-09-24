using Microsoft.AspNetCore.Identity;
using Sgo.Domain.Common;

namespace Sgo.Infrastructure.Identity;

/// <summary>A role is a set of permissions (RN-40). Permissions live in security.role_permission.</summary>
[Audited]
public class AppRole : IdentityRole<Guid>, IVersioned
{
    private AppRole() { }

    public AppRole(string name, string description, bool isSystem)
    {
        Id = Guid.CreateVersion7();
        Name = name;
        Description = description;
        IsSystem = isSystem;
    }

    public string Description { get; set; } = null!;

    /// <summary>Seeded role; editable but not deletable.</summary>
    public bool IsSystem { get; private set; }

    public uint Version { get; private set; }
}
