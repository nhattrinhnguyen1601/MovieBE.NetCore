using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieApi.Application.Common.Auth;
using MovieApi.Application.DTOs.Movies;
using MovieApi.Application.DTOs.Videos;
using MovieApi.Application.Interfaces;

namespace MovieApi.Api.Controllers;

[ApiController]
public sealed class VideosController : ControllerBase
{
    private readonly IVideoService _videoService;

    public VideosController(IVideoService videoService)
    {
        _videoService = videoService;
    }
    [Authorize(Policy = AuthPolicies.EditorOrAdmin)]
    [HttpPost("episodes/{episodeId:long}/videos")]
    public async Task<ActionResult<VideoItem>> Create(
        long episodeId,
        [FromBody] VideoCreateRequest request,
        CancellationToken ct)
    {
        var result = await _videoService.CreateAsync(episodeId, request, ct);
        return Ok(result);
    }
    [Authorize(Policy = AuthPolicies.EditorOrAdmin)]
    [HttpPatch("videos/{id:long}/set-default")]
    public async Task<ActionResult<VideoItem>> SetDefault(
        long id,
        [FromBody] SetDefaultVideoRequest request,
        CancellationToken ct)
    {
        var result = await _videoService.SetDefaultAsync(id, request, ct);
        return Ok(result);
    }
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [HttpDelete("videos/{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _videoService.DeleteAsync(id, ct);
        return NoContent();
    }
}