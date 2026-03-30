namespace MovieApi.Application.DTOs.Episodes;

public sealed class EpisodeCreateRequest
{
    public int EpisodeNumber { get; set; }
    public string Title { get; set; } = default!;
}