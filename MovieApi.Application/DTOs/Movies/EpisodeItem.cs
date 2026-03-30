namespace MovieApi.Application.DTOs.Movies;

public sealed record EpisodeItem(
    long Id,
    int EpisodeNumber,
    string Title,
    IReadOnlyList<VideoItem> Videos
);