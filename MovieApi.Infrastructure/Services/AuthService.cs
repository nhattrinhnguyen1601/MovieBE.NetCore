using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MovieApi.Application.Common.Exceptions;
using MovieApi.Application.Common.Settings;
using MovieApi.Application.DTOs.Auth;
using MovieApi.Application.Interfaces;
using MovieApi.Domain.Entities;
using MovieApi.Domain.Enums;
using MovieApi.Infrastructure.Persistence;

namespace MovieApi.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private const long ViewerRoleId = 3;

    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        IRefreshTokenService refreshTokenService,
        IJwtTokenService jwtTokenService,
        IOptions<JwtSettings> jwtOptions)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _refreshTokenService = refreshTokenService;
        _jwtTokenService = jwtTokenService;
        _jwtSettings = jwtOptions.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        ValidateRegisterRequest(request);

        var email = NormalizeEmail(request.Email);
        var deviceId = NormalizeDeviceId(request.DeviceId);

        var emailExists = await _db.Users
            .AsNoTracking()
            .AnyAsync(x => x.Email == email, ct);

        if (emailExists)
        {
            throw new ConflictException(
                "EMAIL_ALREADY_EXISTS",
                $"Email '{email}' is already registered.");
        }

        var viewerRoleExists = await _db.Roles
            .AsNoTracking()
            .AnyAsync(x => x.Id == ViewerRoleId, ct);

        if (!viewerRoleExists)
        {
            throw new NotFoundException(
                "ROLE_NOT_FOUND",
                "Viewer role was not found. Please ensure seed data is applied.");
        }

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        user.UserRoles.Add(new UserRole
        {
            RoleId = ViewerRoleId
        });

        var plainRefreshToken = _refreshTokenService.GenerateToken();
        var refreshTokenHash = _refreshTokenService.HashToken(plainRefreshToken);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);

        user.RefreshTokens.Add(new RefreshToken
        {
            TokenHash = refreshTokenHash,
            DeviceId = deviceId,
            ExpiresAt = refreshTokenExpiresAt,
            RevokedAt = null,
            CreatedAt = DateTime.UtcNow
        });

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var roles = new[] { "Viewer" };
        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, roles);

        return new AuthResponse(
            user.Id,
            user.Email,
            roles,
            new TokenPairResponse(
                accessToken.AccessToken,
                plainRefreshToken,
                accessToken.ExpiresAt,
                refreshTokenExpiresAt
            )
        );
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        ValidateLoginRequest(request);

        var email = NormalizeEmail(request.Email);
        var deviceId = NormalizeDeviceId(request.DeviceId);

        var user = await _db.Users
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .Include(x => x.RefreshTokens)
            .FirstOrDefaultAsync(x => x.Email == email, ct);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new ValidationException(
                "INVALID_CREDENTIALS",
                new Dictionary<string, string[]>
                {
                    ["email"] = new[] { "Email or password is incorrect." }
                });
        }

        if (user.Status == UserStatus.Locked)
        {
            throw new ValidationException(
                "USER_LOCKED",
                new Dictionary<string, string[]>
                {
                    ["user"] = new[] { "This user account is locked." }
                });
        }

        var roles = user.UserRoles
            .Where(x => x.Role is not null)
            .Select(x => x.Role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, roles);

        var plainRefreshToken = _refreshTokenService.GenerateToken();
        var refreshTokenHash = _refreshTokenService.HashToken(plainRefreshToken);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);

        var existingRefreshToken = user.RefreshTokens
            .FirstOrDefault(x => x.DeviceId == deviceId);

        if (existingRefreshToken is null)
        {
            user.RefreshTokens.Add(new RefreshToken
            {
                TokenHash = refreshTokenHash,
                DeviceId = deviceId,
                ExpiresAt = refreshTokenExpiresAt,
                RevokedAt = null,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existingRefreshToken.TokenHash = refreshTokenHash;
            existingRefreshToken.ExpiresAt = refreshTokenExpiresAt;
            existingRefreshToken.RevokedAt = null;
            existingRefreshToken.CreatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        return new AuthResponse(
            user.Id,
            user.Email,
            roles,
            new TokenPairResponse(
                accessToken.AccessToken,
                plainRefreshToken,
                accessToken.ExpiresAt,
                refreshTokenExpiresAt
            )
        );
    }
    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        ValidateRefreshRequest(request);

        var deviceId = NormalizeDeviceId(request.DeviceId);
        var incomingRefreshTokenHash = _refreshTokenService.HashToken(request.RefreshToken);

        var refreshToken = await _db.RefreshTokens
            .Include(x => x.User)
                .ThenInclude(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.DeviceId == deviceId && x.TokenHash == incomingRefreshTokenHash,ct);

        if (refreshToken is null)
        {
            throw new ValidationException(
                "INVALID_REFRESH_TOKEN",
                new Dictionary<string, string[]>
                {
                    ["refreshToken"] = new[] { "Refresh token is invalid." }
                });
        }

        if (refreshToken.RevokedAt.HasValue)
        {
            throw new ValidationException(
                "INVALID_REFRESH_TOKEN",
                new Dictionary<string, string[]>
                {
                    ["refreshToken"] = new[] { "Refresh token has been revoked." }
                });
        }

        if (refreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new ValidationException(
                "INVALID_REFRESH_TOKEN",
                new Dictionary<string, string[]>
                {
                    ["refreshToken"] = new[] { "Refresh token has expired." }
                });
        }

        var user = refreshToken.User;
        if (user is null)
        {
            throw new ValidationException(
                "INVALID_REFRESH_TOKEN",
                new Dictionary<string, string[]>
                {
                    ["refreshToken"] = new[] { "Refresh token is invalid." }
                });
        }

        if (user.Status == UserStatus.Locked)
        {
            throw new ValidationException(
                "USER_LOCKED",
                new Dictionary<string, string[]>
                {
                    ["user"] = new[] { "This user account is locked." }
                });
        }

        var roles = user.UserRoles
            .Where(x => x.Role is not null)
            .Select(x => x.Role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, roles);

        var newPlainRefreshToken = _refreshTokenService.GenerateToken();
        var newRefreshTokenHash = _refreshTokenService.HashToken(newPlainRefreshToken);
        var newRefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);

        refreshToken.TokenHash = newRefreshTokenHash;
        refreshToken.ExpiresAt = newRefreshTokenExpiresAt;
        refreshToken.RevokedAt = null;
        refreshToken.CreatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new AuthResponse(
            user.Id,
            user.Email,
            roles,
            new TokenPairResponse(
                accessToken.AccessToken,
                newPlainRefreshToken,
                accessToken.ExpiresAt,
                newRefreshTokenExpiresAt
            )
        );
    }

    public async Task LogoutAsync(long userId, LogoutRequest request, CancellationToken ct = default)
    {
        ValidateLogoutRequest(request);

        var deviceId = NormalizeDeviceId(request.DeviceId);

        var refreshToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(
                x => x.UserId == userId &&
                     x.DeviceId == deviceId &&
                     !x.RevokedAt.HasValue,
                ct);

        if (refreshToken is null)
        {
            return;
        }

        refreshToken.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task LogoutAllAsync(long userId, CancellationToken ct = default)
    {
        var refreshTokens = await _db.RefreshTokens
            .Where(x => x.UserId == userId && !x.RevokedAt.HasValue)
            .ToListAsync(ct);

        if (refreshTokens.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;

        foreach (var refreshToken in refreshTokens)
        {
            refreshToken.RevokedAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<MeResponse> GetMeAsync(long userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId, ct);

        if (user is null)
        {
            throw new NotFoundException(
                "USER_NOT_FOUND",
                $"User with id {userId} was not found.");
        }

        var roles = user.UserRoles
            .Where(x => x.Role is not null)
            .Select(x => x.Role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new MeResponse(
            user.Id,
            user.Email,
            roles
        );
    }
    private static void ValidateLogoutRequest(LogoutRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.DeviceId))
            errors["deviceId"] = new[] { "DeviceId is required." };
        else if (request.DeviceId.Trim().Length > 100)
            errors["deviceId"] = new[] { "DeviceId must not exceed 100 characters." };

        if (errors.Count > 0)
            throw new ValidationException("VALIDATION_ERROR", errors);
    }
    private static void ValidateRefreshRequest(RefreshRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            errors["refreshToken"] = new[] { "RefreshToken is required." };

        if (string.IsNullOrWhiteSpace(request.DeviceId))
            errors["deviceId"] = new[] { "DeviceId is required." };
        else if (request.DeviceId.Trim().Length > 100)
            errors["deviceId"] = new[] { "DeviceId must not exceed 100 characters." };

        if (errors.Count > 0)
            throw new ValidationException("VALIDATION_ERROR", errors);
    }
    private static void ValidateLoginRequest(LoginRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email))
            errors["email"] = new[] { "Email is required." };
        else if (request.Email.Trim().Length > 255)
            errors["email"] = new[] { "Email must not exceed 255 characters." };
        else if (!IsValidEmail(request.Email))
            errors["email"] = new[] { "Email is not in a valid format." };

        if (string.IsNullOrWhiteSpace(request.Password))
            errors["password"] = new[] { "Password is required." };

        if (string.IsNullOrWhiteSpace(request.DeviceId))
            errors["deviceId"] = new[] { "DeviceId is required." };
        else if (request.DeviceId.Trim().Length > 100)
            errors["deviceId"] = new[] { "DeviceId must not exceed 100 characters." };

        if (errors.Count > 0)
            throw new ValidationException("VALIDATION_ERROR", errors);
    }
    private static void ValidateRegisterRequest(RegisterRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email))
            errors["email"] = new[] { "Email is required." };
        else if (request.Email.Trim().Length > 255)
            errors["email"] = new[] { "Email must not exceed 255 characters." };
        else if (!IsValidEmail(request.Email))
            errors["email"] = new[] { "Email is not in a valid format." };

        if (string.IsNullOrWhiteSpace(request.Password))
            errors["password"] = new[] { "Password is required." };
        else if (request.Password.Length < 6)
            errors["password"] = new[] { "Password must be at least 6 characters." };

        if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
            errors["confirmPassword"] = new[] { "ConfirmPassword is required." };
        else if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
            errors["confirmPassword"] = new[] { "ConfirmPassword does not match Password." };

        if (string.IsNullOrWhiteSpace(request.DeviceId))
            errors["deviceId"] = new[] { "DeviceId is required." };
        else if (request.DeviceId.Trim().Length > 100)
            errors["deviceId"] = new[] { "DeviceId must not exceed 100 characters." };

        if (errors.Count > 0)
            throw new ValidationException("VALIDATION_ERROR", errors);
    }

    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();

    private static string NormalizeDeviceId(string deviceId)
        => deviceId.Trim();

    private static bool IsValidEmail(string email)
    {
        try
        {
            var _ = new System.Net.Mail.MailAddress(email.Trim());
            return true;
        }
        catch
        {
            return false;
        }
    }
}