namespace MovieApi.Application.DTOs.Auth;

public sealed class RegisterRequest
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string ConfirmPassword { get; set; } = default!;
    public string DeviceId { get; set; } = default!;
}