using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieApi.Application.Common.Auth;
using MovieApi.Application.Common.Models;
using MovieApi.Application.DTOs.AdminUsers;
using MovieApi.Application.Interfaces;

namespace MovieApi.Api.Controllers;

[ApiController]
[Route("admin/users")]
[Authorize(Policy = AuthPolicies.AdminOnly)]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminUserListItemResponse>>> GetUsers(
        [FromQuery] AdminUserQuery query,
        CancellationToken ct)
    {
        var result = await _adminUserService.GetUsersAsync(query, ct);
        return Ok(result);
    }

    [HttpPatch("{id:long}/lock")]
    public async Task<IActionResult> Lock(long id, CancellationToken ct)
    {
        await _adminUserService.LockAsync(id, ct);
        return NoContent();
    }

    [HttpPatch("{id:long}/unlock")]
    public async Task<IActionResult> Unlock(long id, CancellationToken ct)
    {
        await _adminUserService.UnlockAsync(id, ct);
        return NoContent();
    }
}