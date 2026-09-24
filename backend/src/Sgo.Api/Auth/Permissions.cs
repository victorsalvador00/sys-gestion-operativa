using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Sgo.Application.Security;

namespace Sgo.Api.Auth;

/// <summary>Requires the caller to hold <paramref name="permission"/> (dominio §6).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(string permission)
    : AuthorizeAttribute(PermissionPolicyProvider.Prefix + permission)
{
    public string Permission { get; } = permission;
}

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

/// <summary>Builds "perm:&lt;code&gt;" policies on demand instead of registering one per permission.</summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public const string Prefix = "perm:";

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(Prefix, StringComparison.Ordinal))
            return await base.GetPolicyAsync(policyName);

        var permission = policyName[Prefix.Length..];
        if (!Domain.Security.Permissions.IsValid(permission))
            throw new InvalidOperationException($"Unknown permission '{permission}' in [RequirePermission].");

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();
    }
}

public sealed class PermissionAuthorizationHandler(UserAccessContext accessContext)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (accessContext.Access is { IsActive: true } access && access.Has(requirement.Permission))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
