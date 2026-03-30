namespace MovieApi.Application.DTOs.Auth;

public sealed class RefreshRequest
{
    public string RefreshToken { get; set; } = default!;
    public string DeviceId { get; set; } = default!;
}