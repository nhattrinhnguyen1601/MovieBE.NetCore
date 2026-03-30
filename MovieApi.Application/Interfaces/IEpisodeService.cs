using MovieApi.Application.DTOs.Episodes;
using MovieApi.Application.DTOs.Movies;

namespace MovieApi.Application.Interfaces;

public interface IEpisodeService
{
    Task<EpisodeItem> CreateAsync(long movieId, EpisodeCreateRequest request, CancellationToken ct = default);

    Task<EpisodeItem> UpdateAsync(long id, EpisodeUpdateRequest request, CancellationToken ct = default);

    Task DeleteAsync(long id, CancellationToken ct = default);
}