using MovieApi.Application.DTOs.Auth;

namespace MovieApi.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default);

    Task LogoutAsync(long userId, LogoutRequest request, CancellationToken ct = default);

    Task LogoutAllAsync(long userId, CancellationToken ct = default);

    Task<MeResponse> GetMeAsync(long userId, CancellationToken ct = default);
}