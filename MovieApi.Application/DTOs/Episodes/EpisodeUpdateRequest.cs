namespace MovieApi.Application.DTOs.Episodes;

public sealed class EpisodeUpdateRequest
{
    public int EpisodeNumber { get; set; }
    public string Title { get; set; } = default!;
}