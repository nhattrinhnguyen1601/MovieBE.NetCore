namespace MovieApi.Application.DTOs.Movies;

public sealed record CategoryItem(
    long Id,
    string Name,
    string Slug
);