namespace MovieApi.Domain.Entities;

public class Video
{
    public long Id { get; set; }
    public long EpisodeId { get; set; }
    public Episode Episode { get; set; } = default!;

    public string ServerName { get; set; } = default!;
    public string Quality { get; set; } = default!;
    public string Url { get; set; } = default!;
    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}