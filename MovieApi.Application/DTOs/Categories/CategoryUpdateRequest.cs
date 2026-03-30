namespace MovieApi.Application.DTOs.Categories;
public sealed class CategoryUpdateRequest
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
}