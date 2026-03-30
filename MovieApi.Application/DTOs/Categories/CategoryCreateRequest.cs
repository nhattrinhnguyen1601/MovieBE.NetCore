namespace MovieApi.Application.DTOs.Categories;
public sealed class CategoryCreateRequest
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
}