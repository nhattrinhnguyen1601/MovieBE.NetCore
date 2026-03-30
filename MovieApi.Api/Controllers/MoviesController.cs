using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieApi.Application.Common.Auth;
using MovieApi.Application.Common.Models;
using MovieApi.Application.DTOs.Movies;
using MovieApi.Application.Interfaces;

namespace MovieApi.Api.Controllers;

[ApiController]
[Route("movies")]
public sealed class MoviesController : ControllerBase
{
    private readonly IMovieService _movieService;

    public MoviesController(IMovieService movieService)
    {
        _movieService = movieService;
    }
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PagedResult<MovieListItemResponse>>> GetMovies(
        [FromQuery] MovieQuery query,
        CancellationToken ct)
    {
        var result = await _movieService.GetMoviesAsync(query, ct);
        return Ok(result);
    }
    [AllowAnonymous]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<MovieDetailResponse>> GetById(long id, CancellationToken ct)
    {
        var result = await _movieService.GetByIdAsync(id, ct);
        return Ok(result);
    }
    [Authorize(Policy = AuthPolicies.EditorOrAdmin)]
    [HttpPost]
    public async Task<ActionResult<MovieDetailResponse>> Create(
        [FromBody] MovieCreateRequest request,
        CancellationToken ct)
    {
        var result = await _movieService.CreateAsync(request, ct);
        return Ok(result);
    }

    [Authorize(Policy = AuthPolicies.EditorOrAdmin)]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<MovieDetailResponse>> Update(
        long id,
        [FromBody] MovieUpdateRequest request,
        CancellationToken ct)
    {
        var result = await _movieService.UpdateAsync(id, request, ct);
        return Ok(result);
    }
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _movieService.DeleteAsync(id, ct);
        return NoContent();
    }
}