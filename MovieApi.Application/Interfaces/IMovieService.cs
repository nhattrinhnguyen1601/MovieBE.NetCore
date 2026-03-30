using MovieApi.Application.Common.Models;
using MovieApi.Application.DTOs.Movies;

namespace MovieApi.Application.Interfaces;

public interface IMovieService
{
    Task<PagedResult<MovieListItemResponse>> GetMoviesAsync(MovieQuery query, CancellationToken ct = default);
    Task<MovieDetailResponse> GetByIdAsync(long id, CancellationToken ct = default);

    Task<MovieDetailResponse> CreateAsync(MovieCreateRequest request, CancellationToken ct = default);

    Task<MovieDetailResponse> UpdateAsync(long id, MovieUpdateRequest request, CancellationToken ct = default);

    Task DeleteAsync(long id, CancellationToken ct = default);
}