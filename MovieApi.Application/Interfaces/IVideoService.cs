using MovieApi.Application.DTOs.Movies;
using MovieApi.Application.DTOs.Videos;

namespace MovieApi.Application.Interfaces;

public interface IVideoService
{
    Task<VideoItem> CreateAsync(long episodeId, VideoCreateRequest request, CancellationToken ct = default);

    Task<VideoItem> SetDefaultAsync(long id, SetDefaultVideoRequest request, CancellationToken ct = default);

    Task DeleteAsync(long id, CancellationToken ct = default);
}