namespace MovieApi.Application.DTOs.Movies;

public sealed record MovieDetailResponse(
    long Id,
    string Title,
    string? Description,
    int Year,
    string Type,
    IReadOnlyList<CategoryItem> Categories,
    IReadOnlyList<EpisodeItem> Episodes
);