namespace MovieApi.Application.Common.Settings;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public string Key { get; set; } = default!;

    public int AccessTokenMinutes { get; set; }
    public int RefreshTokenDays { get; set; }

    public string SeedAdminEmail { get; set; } = default!;
    public string SeedAdminPassword { get; set; } = default!;
}