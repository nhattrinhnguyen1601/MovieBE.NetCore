namespace MovieApi.Application.DTOs.Auth;

public sealed record AuthResponse(
    long UserId,
    string Email,
    IReadOnlyList<string> Roles,
    TokenPairResponse Tokens
);