namespace MovieApi.Application.DTOs.Movies;

public sealed record MovieListItemResponse(
    long Id,
    string Title,
    int Year,
    string Type
);