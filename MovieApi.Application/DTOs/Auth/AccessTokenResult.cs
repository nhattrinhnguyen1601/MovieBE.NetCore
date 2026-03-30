namespace MovieApi.Application.DTOs.Auth;

public sealed record AccessTokenResult(
    string AccessToken,
    DateTime ExpiresAt
);