namespace MovieApi.Domain.Entities;

public class Movie
{
    public long Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public int Year { get; set; }
    public string Type { get; set; } = "Movie"; // Movie/Series (tạm)

    public long CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<MovieCategory> MovieCategories { get; set; } = new();
    public List<Episode> Episodes { get; set; } = new();
}
