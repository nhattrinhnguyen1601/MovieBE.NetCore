namespace MovieApi.Application.DTOs.Auth;

public sealed class LoginRequest
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string DeviceId { get; set; } = default!;
}