namespace MovieApi.Domain.Entities;

public class Category
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;

    public List<MovieCategory> MovieCategories { get; set; } = new();
}