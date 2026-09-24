using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sgo.Application.Common;
using Sgo.Application.Security;
using Sgo.Domain.Common;
using Sgo.Domain.Security;
using Sgo.Infrastructure.Persistence;

namespace Sgo.Infrastructure.Identity;

public sealed class AuthService(
    SgoDbContext db,
    UserManager<AppUser> userManager,
    TokenService tokens,
    IUserAccessService userAccess,
    IClock clock,
    ICurrentUser currentUser) : IAuthService
{
    public const string InvalidCredentialsMessage = "Correo o contraseña incorrectos.";
    public const string LockedMessage = "La cuenta está bloqueada por intentos fallidos. Intenta de nuevo en 15 minutos.";
    public const string InactiveMessage = "El usuario está inactivo. Contacta al administrador.";
    public const string SessionExpiredMessage = "Tu sesión expiró. Inicia sesión de nuevo.";

    private static AuthenticationFailedException InvalidCredentials() => new("invalid_credentials", InvalidCredentialsMessage);
    private static AuthenticationFailedException Locked() => new("account_locked", LockedMessage);
    private static AuthenticationFailedException SessionExpired() => new("session_expired", SessionExpiredMessage);

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email) ?? throw InvalidCredentials();

        if (await userManager.IsLockedOutAsync(user))
            throw Locked();

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user); // RN-42: locks after 5 failures
            throw await userManager.IsLockedOutAsync(user) ? Locked() : InvalidCredentials();
        }

        if (!user.IsActive)
            throw new AuthenticationFailedException("user_inactive", InactiveMessage);

        await userManager.ResetAccessFailedCountAsync(user);

        var (refreshToken, hash, expiresAt) = tokens.CreateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken(user.Id, hash, Guid.CreateVersion7(), clock.UtcNow, expiresAt, currentUser.IpAddress));
        await db.SaveChangesAsync(ct);

        return new AuthResult(tokens.CreateAccessToken(user), refreshToken, expiresAt);
    }

    public async Task<AuthResult> RefreshAsync(string? refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw SessionExpired();

        var now = clock.UtcNow;
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == TokenService.Hash(refreshToken), ct)
                     ?? throw SessionExpired();

        if (stored.WasRotated)
        {
            // A rotated token came back: assume it was stolen and kill the whole session family.
            await RevokeFamilyAsync(stored.FamilyId, RefreshTokenRevocation.ReuseDetected, ct);
            throw SessionExpired();
        }

        if (!stored.IsActive(now))
            throw SessionExpired();

        var user = await userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null || !user.IsActive || await userManager.IsLockedOutAsync(user))
        {
            await RevokeFamilyAsync(stored.FamilyId, RefreshTokenRevocation.UserUnavailable, ct);
            throw SessionExpired();
        }

        var (newToken, hash, expiresAt) = tokens.CreateRefreshToken();
        db.RefreshTokens.Add(stored.Rotate(hash, now, expiresAt, currentUser.IpAddress));

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request rotated the same token first.
            throw SessionExpired();
        }

        return new AuthResult(tokens.CreateAccessToken(user), newToken, expiresAt);
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var stored = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == TokenService.Hash(refreshToken), ct);
        if (stored is not null && stored.UserId == currentUser.UserId)
            await RevokeFamilyAsync(stored.FamilyId, RefreshTokenRevocation.Logout, ct);
    }

    public async Task<MeDto> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new NotFoundException("el usuario", userId);
        var access = await userAccess.GetAsync(userId, ct) ?? throw new NotFoundException("el usuario", userId);

        var locations = await db.Locations.AsNoTracking()
            .Where(l => access.LocationIds.Contains(l.Id))
            .OrderBy(l => l.Code)
            .Select(l => new MeLocationDto(l.Id, l.Code, l.Name, l.Type.ToString(), l.IsActive))
            .ToListAsync(ct);

        return new MeDto(user.Id, user.Email!, user.FullName, user.DefaultLocationId,
            access.Permissions.Order().ToList(), access.AllLocations, locations);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()) ?? throw new NotFoundException("el usuario", userId);

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var mismatch = result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch));
            throw new RequestValidationException(mismatch
                ? new Dictionary<string, string[]> { ["currentPassword"] = ["La contraseña actual es incorrecta."] }
                : new Dictionary<string, string[]> { ["newPassword"] = result.Errors.Select(e => e.Description).ToArray() });
        }

        var now = clock.UtcNow;
        var active = await db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync(ct);
        active.ForEach(t => t.Revoke(now, RefreshTokenRevocation.PasswordChanged));
        await db.SaveChangesAsync(ct);
    }

    private async Task RevokeFamilyAsync(Guid familyId, RefreshTokenRevocation reason, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var family = await db.RefreshTokens.Where(t => t.FamilyId == familyId && t.RevokedAt == null).ToListAsync(ct);
        family.ForEach(t => t.Revoke(now, reason));
        await db.SaveChangesAsync(ct);
    }
}
