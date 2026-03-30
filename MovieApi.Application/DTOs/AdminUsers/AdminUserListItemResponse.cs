namespace MovieApi.Application.DTOs.AdminUsers;

public sealed record AdminUserListItemResponse(
    long Id,
    string Email,
    string Status,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt
);