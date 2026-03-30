using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieApi.Application.Common.Auth;
using MovieApi.Application.DTOs.Episodes;
using MovieApi.Application.DTOs.Movies;
using MovieApi.Application.Interfaces;

namespace MovieApi.Api.Controllers;

[ApiController]
public sealed class EpisodesController : ControllerBase
{
    private readonly IEpisodeService _episodeService;

    public EpisodesController(IEpisodeService episodeService)
    {
        _episodeService = episodeService;
    }
    [Authorize(Policy = AuthPolicies.EditorOrAdmin)]
    [HttpPost("movies/{movieId:long}/episodes")]
    public async Task<ActionResult<EpisodeItem>> Create(
        long movieId,
        [FromBody] EpisodeCreateRequest request,
        CancellationToken ct)
    {
        var result = await _episodeService.CreateAsync(movieId, request, ct);
        return Ok(result);
    }
    [Authorize(Policy = AuthPolicies.EditorOrAdmin)]
    [HttpPut("episodes/{id:long}")]
    public async Task<ActionResult<EpisodeItem>> Update(
        long id,
        [FromBody] EpisodeUpdateRequest request,
        CancellationToken ct)
    {
        var result = await _episodeService.UpdateAsync(id, request, ct);
        return Ok(result);
    }
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [HttpDelete("episodes/{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _episodeService.DeleteAsync(id, ct);
        return NoContent();
    }
}