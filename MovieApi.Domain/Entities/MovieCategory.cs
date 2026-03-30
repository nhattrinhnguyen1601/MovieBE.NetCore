namespace MovieApi.Domain.Entities;

public class MovieCategory
{
    public long MovieId { get; set; }
    public Movie Movie { get; set; } = default!;

    public long CategoryId { get; set; }
    public Category Category { get; set; } = default!;
}