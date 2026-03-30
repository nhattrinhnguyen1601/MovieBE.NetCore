namespace MovieApi.Application.DTOs.Auth;

public sealed record TokenPairResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt
);