namespace MovieApi.Application.DTOs.Categories;

public sealed record CategoryResponse(
    long Id,
    string Name,
    string Slug
);