using MovieApi.Application.DTOs.Categories;

namespace MovieApi.Application.Interfaces;
public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken ct = default);

    Task<CategoryResponse> CreateAsync(CategoryCreateRequest request, CancellationToken ct = default);

    Task<CategoryResponse> UpdateAsync(long id, CategoryUpdateRequest request, CancellationToken ct = default);

    Task DeleteAsync(long id, CancellationToken ct = default);
}