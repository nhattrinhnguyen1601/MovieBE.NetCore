using Microsoft.EntityFrameworkCore;
using MovieApi.Application.Common.Exceptions;
using MovieApi.Application.Common.Models;
using MovieApi.Application.DTOs.Movies;
using MovieApi.Application.Interfaces;
using MovieApi.Domain.Entities;
using MovieApi.Infrastructure.Persistence;
using MovieApi.Application.DTOs.Audit;
using Hangfire;

namespace MovieApi.Infrastructure.Services;

public sealed class MovieService : IMovieService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    public MovieService(
        AppDbContext db,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _db = db;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<MovieListItemResponse>> GetMoviesAsync(MovieQuery query, CancellationToken ct = default)
    {
        var moviesQuery = _db.Movies
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            moviesQuery = moviesQuery.Where(x => x.Title.Contains(search));
        }

        if (query.Year.HasValue)
        {
            moviesQuery = moviesQuery.Where(x => x.Year == query.Year.Value);
        }

        moviesQuery = ApplySorting(moviesQuery, query.SortBy, query.Order);

        var totalItems = await moviesQuery.CountAsync(ct);

        var items = await moviesQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new MovieListItemResponse(
                x.Id,
                x.Title,
                x.Year,
                x.Type
            ))
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize);

        return new PagedResult<MovieListItemResponse>(
            items,
            query.Page,
            query.PageSize,
            totalItems,
            totalPages
        );
    }

    public async Task<MovieDetailResponse> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.Movies
            .AsNoTracking()
            .Include(x => x.MovieCategories)
                .ThenInclude(x => x.Category)
            .Include(x => x.Episodes)
                .ThenInclude(x => x.Videos)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
        {
            throw new NotFoundException(
                "MOVIE_NOT_FOUND",
                $"Movie with id {id} was not found.");
        }

        return MapToDetail(entity);
    }

    public async Task<MovieDetailResponse> CreateAsync(MovieCreateRequest request, CancellationToken ct = default)
    {
        ValidateMovieInput(request.Title, request.Year, request.Type);

        var normalizedCategoryIds = NormalizeCategoryIds(request.CategoryIds);
        await EnsureCategoriesExistAsync(normalizedCategoryIds, ct);

        var entity = new Movie
        {
            Title = NormalizeTitle(request.Title),
            Description = NormalizeDescription(request.Description),
            Year = request.Year,
            Type = NormalizeType(request.Type),
            CreatedBy = 0, // tạm hard-code, phase auth sau sẽ lấy từ current user
            CreatedAt = DateTime.UtcNow
        };
        foreach (var categoryId in normalizedCategoryIds)
        {
            entity.MovieCategories.Add(new MovieCategory
            {
                CategoryId = categoryId
            });
        }

        _db.Movies.Add(entity);
        await _db.SaveChangesAsync(ct);
        var currentUser = _currentUserService.GetCurrentUser();

        await _auditService.EnqueueAsync(
            new AuditLogRequest(
                EventId: Guid.NewGuid().ToString("N"),
                ActorUserId: currentUser.UserId ?? 0,
                Action: "CREATE",
                Entity: "Movie",
                EntityId: entity.Id,
                Payload: new
                {
                    entity.Id,
                    entity.Title,
                    entity.Description,
                    entity.Year,
                    entity.Type,
                    CategoryIds = normalizedCategoryIds
                }),ct);
        await _db.Entry(entity)
                    .Collection(x => x.MovieCategories)
                    .Query()
                    .Include(x => x.Category)
                    .LoadAsync(ct);

        BackgroundJob.Enqueue<NotifyJob>(x =>
            x.ExecuteAsync($"Movie created: {entity.Title} (Id={entity.Id})"));

        return MapToDetail(entity);
    }

    public async Task<MovieDetailResponse> UpdateAsync(long id, MovieUpdateRequest request, CancellationToken ct = default)
    {
        ValidateMovieInput(request.Title, request.Year, request.Type);

        var normalizedCategoryIds = NormalizeCategoryIds(request.CategoryIds);
        await EnsureCategoriesExistAsync(normalizedCategoryIds, ct);

        var entity = await _db.Movies
            .Include(x => x.MovieCategories)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
        {
            throw new NotFoundException(
                "MOVIE_NOT_FOUND",
                $"Movie with id {id} was not found.");
        }

        entity.Title = NormalizeTitle(request.Title);
        entity.Description = NormalizeDescription(request.Description);
        entity.Year = request.Year;
        entity.Type = NormalizeType(request.Type);
        entity.UpdatedAt = DateTime.UtcNow;

        _db.MovieCategories.RemoveRange(entity.MovieCategories);
        entity.MovieCategories.Clear();

        foreach (var categoryId in normalizedCategoryIds)
        {
            entity.MovieCategories.Add(new MovieCategory
            {
                MovieId = entity.Id,
                CategoryId = categoryId
            });
        }

        await _db.SaveChangesAsync(ct);
        var currentUser = _currentUserService.GetCurrentUser();

        await _auditService.EnqueueAsync(
            new AuditLogRequest(
                EventId: Guid.NewGuid().ToString("N"),
                ActorUserId: currentUser.UserId ?? 0,
                Action: "UPDATE",
                Entity: "Movie",
                EntityId: entity.Id,
                Payload: new
                {
                    entity.Id,
                    entity.Title,
                    entity.Description,
                    entity.Year,
                    entity.Type,
                    CategoryIds = normalizedCategoryIds
                }), ct);
        await _db.Entry(entity)
            .Collection(x => x.MovieCategories)
            .Query()
            .Include(x => x.Category)
            .LoadAsync(ct);
        BackgroundJob.Enqueue<NotifyJob>(x =>
            x.ExecuteAsync($"Movie updated: {entity.Title} (Id={entity.Id})"));
        return MapToDetail(entity);
    }
    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.Movies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
        {
            throw new NotFoundException(
                "MOVIE_NOT_FOUND",
                $"Movie with id {id} was not found.");
        }

        var payload = new
        {
            entity.Id,
            entity.Title,
            entity.Description,
            entity.Year,
            entity.Type
        };

        var currentUser = _currentUserService.GetCurrentUser();

        _db.Movies.Attach(entity);
        _db.Movies.Remove(entity);

        await _db.SaveChangesAsync(ct);

        await _auditService.EnqueueAsync(
            new AuditLogRequest(
                EventId: Guid.NewGuid().ToString("N"),
                ActorUserId: currentUser.UserId ?? 0,
                Action: "DELETE",
                Entity: "Movie",
                EntityId: entity.Id,
                Payload: payload),
            ct);
        BackgroundJob.Enqueue<NotifyJob>(x =>
            x.ExecuteAsync($"Movie deleted: {payload.Title} (Id={payload.Id})"));
    }

    private static MovieDetailResponse MapToDetail(Movie entity)
    {
        var categories = entity.MovieCategories
            .Where(x => x.Category is not null)
            .Select(x => new CategoryItem(
                x.CategoryId,
                x.Category.Name,
                x.Category.Slug
            ))
            .OrderBy(x => x.Name)
            .ToArray();
        var episodes =  entity.Episodes
            .OrderBy(x => x.EpisodeNumber)
            .ThenBy(x => x.Id)
            .Select(x => new EpisodeItem(
                x.Id,
                x.EpisodeNumber,
                x.Title,
                x.Videos
                    .OrderByDescending(v => v.IsDefault)
                    .ThenBy(v => v.ServerName)
                    .ThenBy(v => v.Id)
                    .Select(v => new VideoItem(
                        v.Id,
                        v.ServerName,
                        v.Quality,
                        v.Url,
                        v.IsDefault
                    )).ToArray()
            )).ToArray();
        return new MovieDetailResponse(
            entity.Id,
            entity.Title,
            entity.Description,
            entity.Year,
            entity.Type,
            categories,
            episodes
        );
    }
    private async Task EnsureCategoriesExistAsync(IReadOnlyList<long> categoryIds, CancellationToken ct)
    {
        if (categoryIds.Count == 0)
            return;

        var existingIds = await _db.Categories
            .Where(x => categoryIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(ct);

        var missingIds = categoryIds.Except(existingIds).ToArray();
        if (missingIds.Length > 0)
        {
            throw new ValidationException(
                "INVALID_CATEGORY_IDS",
                new Dictionary<string, string[]>
                {
                    ["categoryIds"] = new[]
                    {
                        $"These category ids do not exist: {string.Join(", ", missingIds)}"
                    }
                });
        }
    }
    private static IQueryable<Movie> ApplySorting(
        IQueryable<Movie> query,
        string? sortBy,
        string? order)
    {
        var normalizedSortBy = (sortBy ?? "createdAt").Trim().ToLowerInvariant();
        var normalizedOrder = (order ?? "desc").Trim().ToLowerInvariant();

        var isAsc = normalizedOrder == "asc";

        return normalizedSortBy switch
        {
            "title" => isAsc
                ? query.OrderBy(x => x.Title).ThenBy(x => x.Id)
                : query.OrderByDescending(x => x.Title).ThenByDescending(x => x.Id),

            "year" => isAsc
                ? query.OrderBy(x => x.Year).ThenBy(x => x.Id)
                : query.OrderByDescending(x => x.Year).ThenByDescending(x => x.Id),

            "createdat" => isAsc
                ? query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
                : query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id),

            _ => isAsc
                ? query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
                : query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
        };
    }

    private static void ValidateMovieInput(string? title, int year, string? type)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(title))
            errors["title"] = new[] { "Title is required." };
        else if (title.Trim().Length > 300)
            errors["title"] = new[] { "Title must not exceed 300 characters." };

        if (year < 1888 || year > 3000)
            errors["year"] = new[] { "Year must be between 1888 and 3000." };

        var normalizedType = NormalizeType(type);
        if (normalizedType is not ("Movie" or "Series"))
            errors["type"] = new[] { "Type must be either 'Movie' or 'Series'." };

        if (errors.Count > 0)
            throw new ValidationException("VALIDATION_ERROR", errors);
    }
    private static IReadOnlyList<long> NormalizeCategoryIds(IEnumerable<long>? categoryIds)
    {
        return (categoryIds ?? Array.Empty<long>())
            .Where(x => x > 0)
            .Distinct()
            .ToArray();
    }
    private static string NormalizeTitle(string? title)
        => (title ?? "").Trim();

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        return description.Trim();
    }

    private static string NormalizeType(string? type)
    {
        var value = (type ?? "").Trim();

        if (value.Equals("movie", StringComparison.OrdinalIgnoreCase))
            return "Movie";

        if (value.Equals("series", StringComparison.OrdinalIgnoreCase))
            return "Series";

        return value;
    }
}