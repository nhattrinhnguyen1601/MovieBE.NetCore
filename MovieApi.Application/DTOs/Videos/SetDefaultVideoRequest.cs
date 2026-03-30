namespace MovieApi.Application.DTOs.Videos;

public sealed class SetDefaultVideoRequest
{
    public bool IsDefault { get; set; } = true;
}