namespace MovieApi.Domain.Entities;

public class Episode
{
    public long Id { get; set; }
    public long MovieId { get; set; }
    public Movie Movie { get; set; } = default!;

    public int EpisodeNumber { get; set; }
    public string Title { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Video> Videos { get; set; } = new();
}