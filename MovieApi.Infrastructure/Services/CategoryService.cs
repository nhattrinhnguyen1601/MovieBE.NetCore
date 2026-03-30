using Hangfire;
using Microsoft.EntityFrameworkCore;
using MovieApi.Application.Common.Exceptions;
using MovieApi.Application.DTOs.Audit;
using MovieApi.Application.DTOs.Categories;
using MovieApi.Application.Interfaces;
using MovieApi.Domain.Entities;
using MovieApi.Infrastructure.Persistence;
namespace MovieApi.Infrastructure.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    public CategoryService(AppDbContext db, IAuditService auditService, ICurrentUserService currentUserService)
    {
        _db = db;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }
    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _db.Categories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new CategoryResponse(x.Id, x.Name, x.Slug))
            .ToListAsync(ct);

        return items;
    }
    public async Task<CategoryResponse> CreateAsync(CategoryCreateRequest request, CancellationToken ct = default)
    {
        ValidateCategoryInput(request.Name, request.Slug);
        var name = NormalizeName(request.Name);
        var slug = NormalizeSlug(request.Slug);

        var entity = new Category { Name = name, Slug = slug };        
        _db.Categories.Add(entity);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueSlugViolation(ex))
        {
            throw new ConflictException(
                "CATEGORY_SLUG_DUPLICATE",
                $"Category slug '{slug}' already exists.");
        }
        var currentUser = _currentUserService.GetCurrentUser();
        await _auditService.EnqueueAsync(
            new AuditLogRequest(
                EventId: Guid.NewGuid().ToString("N"),
                ActorUserId: currentUser.UserId ?? 0,
                Action: "CREATE",
                Entity: "Category",
                EntityId: entity.Id,
                Payload: new
                {
                    entity.Id,
                    entity.Name,
                    entity.Slug
                }),
            ct);
        BackgroundJob.Enqueue<NotifyJob>(x =>
                x.ExecuteAsync($"Category created: {entity.Name} (Id={entity.Id})"));
        return new CategoryResponse(entity.Id, entity.Name, entity.Slug);

    }
    public async Task<CategoryResponse> UpdateAsync(long id, CategoryUpdateRequest request, CancellationToken ct = default)
    {
        ValidateCategoryInput(request.Name, request.Slug);
        var entity = await _db.Categories.FindAsync(new object[] { id }, ct);
        if (entity == null)
        {
            throw new NotFoundException(
                "CATEGORY_NOT_FOUND",
                $"Category with id {id} was not found.");
        }
        entity.Name = NormalizeName(request.Name);
        entity.Slug = NormalizeSlug(request.Slug);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueSlugViolation(ex))
        {
            throw new ConflictException(
                "CATEGORY_SLUG_DUPLICATE",
                $"Category slug '{entity.Slug}' already exists.");
        }
        var currentUser = _currentUserService.GetCurrentUser();

        await _auditService.EnqueueAsync(
            new AuditLogRequest(
                EventId: Guid.NewGuid().ToString("N"),
                ActorUserId: currentUser.UserId ?? 0,
                Action: "UPDATE",
                Entity: "Category",
                EntityId: entity.Id,
                Payload: new
                {
                    entity.Id,
                    entity.Name,
                    entity.Slug
                }),
            ct);

        BackgroundJob.Enqueue<NotifyJob>(x =>
            x.ExecuteAsync($"Category updated: {entity.Name} (Id={entity.Id})"));
        return new CategoryResponse(entity.Id, entity.Name, entity.Slug);
    }
    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.Categories.FindAsync(new object[] { id }, ct);
        if (entity == null)
        {
            throw new NotFoundException(
                "CATEGORY_NOT_FOUND",
                $"Category with id {id} was not found.");
        }
        var payload = new
        {
            entity.Id,
            entity.Name,
            entity.Slug
        };

        var currentUser = _currentUserService.GetCurrentUser();

        _db.Categories.Remove(entity);
        await _db.SaveChangesAsync(ct);

        await _auditService.EnqueueAsync(
            new AuditLogRequest(
                EventId: Guid.NewGuid().ToString("N"),
                ActorUserId: currentUser.UserId ?? 0,
                Action: "DELETE",
                Entity: "Category",
                EntityId: entity.Id,
                Payload: payload),
            ct);

        BackgroundJob.Enqueue<NotifyJob>(x =>
            x.ExecuteAsync($"Category deleted: {payload.Name} (Id={payload.Id})"));
    }
    private static void ValidateCategoryInput(string? name, string? slug)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
            errors["name"] = new[] { "Name is required." };
        else if (name.Trim().Length > 100)
            errors["name"] = new[] { "Name must not exceed 100 characters." };

        if (string.IsNullOrWhiteSpace(slug))
            errors["slug"] = new[] { "Slug is required." };
        else if (slug.Trim().Length > 120)
            errors["slug"] = new[] { "Slug must not exceed 120 characters." };

        if (errors.Count > 0)
            throw new ValidationException("VALIDATION_ERROR", errors);
    }
    private static string NormalizeName(string? name)
        => (name ?? "").Trim();

    private static string NormalizeSlug(string? slug)
        => (slug ?? "").Trim().ToLowerInvariant();
    private static bool IsUniqueSlugViolation(DbUpdateException ex)
    => ex.InnerException?.Message?.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase) == true;
}