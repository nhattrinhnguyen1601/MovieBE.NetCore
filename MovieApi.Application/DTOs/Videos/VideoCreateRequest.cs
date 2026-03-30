namespace MovieApi.Application.DTOs.Videos;

public sealed class VideoCreateRequest
{
    public string ServerName { get; set; } = default!;
    public string Quality { get; set; } = default!;
    public string Url { get; set; } = default!;

    // optional: nếu client không gửi thì service sẽ tự xử lý default
    public bool IsDefault { get; set; }
}