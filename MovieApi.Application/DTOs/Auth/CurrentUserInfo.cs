namespace MovieApi.Application.DTOs.Auth;

public sealed record CurrentUserInfo(
    bool IsAuthenticated,
    long? UserId,
    string? Email,
    IReadOnlyList<string> Roles
);