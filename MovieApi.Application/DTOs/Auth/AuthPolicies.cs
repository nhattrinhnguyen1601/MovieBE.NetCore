namespace MovieApi.Application.Common.Auth;

public static class AuthPolicies
{
    public const string AdminOnly = nameof(AdminOnly);
    public const string EditorOrAdmin = nameof(EditorOrAdmin);
}