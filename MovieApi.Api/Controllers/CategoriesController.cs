using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieApi.Application.Common.Auth;
using MovieApi.Application.DTOs.Categories;
using MovieApi.Application.Interfaces;

namespace MovieApi.Api.Controllers;

[ApiController]
[Route("categories")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetAll(CancellationToken ct)
    {
        var result = await _categoryService.GetAllAsync(ct);
        return CreatedAtAction(nameof(GetAll), result);
    }
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(
        [FromBody] CategoryCreateRequest request,
        CancellationToken ct)
    {
        var result = await _categoryService.CreateAsync(request, ct);
        return Ok(result);
    }
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<CategoryResponse>> Update(
        long id,
        [FromBody] CategoryUpdateRequest request,
        CancellationToken ct)
    {
        var result = await _categoryService.UpdateAsync(id, request, ct);
        return Ok(result);
    }
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _categoryService.DeleteAsync(id, ct);
        return NoContent();
    }
}