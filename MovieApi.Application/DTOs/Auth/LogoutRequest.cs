namespace MovieApi.Application.DTOs.Auth;

public sealed class LogoutRequest
{
    public string DeviceId { get; set; } = default!;
}