using MovieApi.Application.DTOs.Auth;

namespace MovieApi.Application.Interfaces;

public interface IJwtTokenService
{
    AccessTokenResult GenerateAccessToken(
        long userId,
        string email,
        IReadOnlyList<string> roles);
}