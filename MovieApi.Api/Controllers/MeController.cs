using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieApi.Application.DTOs.Auth;
using MovieApi.Application.Interfaces;

namespace MovieApi.Api.Controllers;

[ApiController]
[Route("me")]
public sealed class MeController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;

    public MeController(
        IAuthService authService,
        ICurrentUserService currentUserService)
    {
        _authService = authService;
        _currentUserService = currentUserService;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<MeResponse>> GetMe(CancellationToken ct)
    {
        var currentUser = _currentUserService.GetCurrentUser();

        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Unauthorized();

        var result = await _authService.GetMeAsync(currentUser.UserId.Value, ct);
        return Ok(result);
    }
}