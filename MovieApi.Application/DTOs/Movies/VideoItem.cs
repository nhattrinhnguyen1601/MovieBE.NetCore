namespace MovieApi.Application.DTOs.Movies;

public sealed record VideoItem(
    long Id,
    string ServerName,
    string Quality,
    string Url,
    bool IsDefault
);