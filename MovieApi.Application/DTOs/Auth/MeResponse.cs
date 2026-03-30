namespace MovieApi.Application.DTOs.Auth;

public sealed record MeResponse(
    long UserId,
    string Email,
    IReadOnlyList<string> Roles
);