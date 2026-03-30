namespace MovieApi.Application.DTOs.Movies;

public sealed class MovieUpdateRequest
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public int Year { get; set; }
    public string Type { get; set; } = "Movie";

    public List<long> CategoryIds { get; set; } = new();
}