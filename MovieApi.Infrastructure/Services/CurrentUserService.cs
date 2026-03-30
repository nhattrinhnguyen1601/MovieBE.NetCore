using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MovieApi.Application.DTOs.Auth;
using MovieApi.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace MovieApi.Infrastructure.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CurrentUserInfo GetCurrentUser()
    {
        var principal = _httpContextAccessor.HttpContext?.User;

        if (principal?.Identity?.IsAuthenticated != true)
        {
            return new CurrentUserInfo(
                IsAuthenticated: false,
                UserId: null,
                Email: null,
                Roles: Array.Empty<string>());
        }

        var userId = TryGetUserId(principal);
        var email = TryGetEmail(principal);
        var roles = principal.FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new CurrentUserInfo(
            IsAuthenticated: true,
            UserId: userId,
            Email: email,
            Roles: roles);
    }

    private static long? TryGetUserId(ClaimsPrincipal principal)
    {
        var value =
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
            principal.FindFirst("sub")?.Value;

        return long.TryParse(value, out var userId) ? userId : null;
    }

    private static string? TryGetEmail(ClaimsPrincipal principal)
    {
        return
            principal.FindFirst(ClaimTypes.Email)?.Value ??
            principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value ??
            principal.FindFirst("email")?.Value;
    }
}