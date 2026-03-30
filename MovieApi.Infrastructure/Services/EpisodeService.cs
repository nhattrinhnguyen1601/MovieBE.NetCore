using Microsoft.EntityFrameworkCore;
using MovieApi.Application.Common.Exceptions;
using MovieApi.Application.DTOs.Episodes;
using MovieApi.Application.DTOs.Movies;
using MovieApi.Application.Interfaces;
using MovieApi.Domain.Entities;
using MovieApi.Infrastructure.Persistence;
using Hangfire;
using MovieApi.Application.DTOs.Audit;

namespace MovieApi.Infrastructure.Services;

public sealed class EpisodeService : IEpisodeService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public EpisodeService(AppDbContext db, IAuditService auditService, ICurrentUserService currentUserService)
    {
        _db = db;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<EpisodeItem> CreateAsync(long movieId, EpisodeCreateRequest request, CancellationToken ct = default)
    {
        ValidateEpisodeInput(request.EpisodeNumber, request.Title);

        var movieExists = await _db.Movies
            .AsNoTracking()
            .AnyAsync(x => x.Id == movieId, ct);

        if (!movieExists)
        {
            throw new NotFoundException(
                "MOVIE_NOT_FOUND",
                $"Movie with id {movieId} was not found.");
        }

        var duplicateExists = await _db.Episodes
            .AsNoTracking()
            .AnyAsync(x => x.MovieId == movieId && x.EpisodeNumber == request.EpisodeNumber, ct);

        if (duplicateExists)
        {
            throw new ConflictException(
                "EPISODE_NUMBER_DUPLICATE",
                $"Episode number {request.EpisodeNumber} already exists for movie id {movieId}.");
        }

        var entity = new Episode
        {
            MovieId = movieId,
            EpisodeNumber = request.EpisodeNumber,
            Title = NormalizeTitle(request.Title),
            CreatedAt = DateTime.UtcNow
        };

        _db.Episodes.Add(entity);
        await _db.SaveChangesAsync(ct);
        var currentUser = _currentUserService.GetCurrentUser();

        await _auditService.EnqueueAsync(
            new AuditLogRequest(
                EventId: Guid.NewGuid().ToString("N"),
                ActorUserId: currentUser.UserId ?? 0,
                Action: "CREATE",
                Entity: "Episode",
                EntityId: entity.Id,
                Payload: new
                {
                    entity.Id,
                    entity.MovieId,
                    entity.EpisodeNumber,
                    entity.Title
                }),
            ct);

        BackgroundJob.Enqueue<NotifyJob>(x =>
            x.ExecuteAsync(
                $"Episode created: {entity.Title} (Id={entity.Id}, MovieId={entity.MovieId}, EpisodeNumber={entity.EpisodeNumber})"));
        return MapToItem(entity);
    }

    public async Task<EpisodeItem> UpdateAsync(long id, EpisodeUpdateRequest request, CancellationToken ct = default)
    {
        ValidateEpisodeInput(request.EpisodeNumber, request.Title);

        var entity = await _db.Episodes
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
        {
            throw new NotFoundException(
                "EPISODE_NOT_FOUND",
                $"Episode with id {id} was not found.");
        }

        var duplicateExists = await _db.Episodes
            .AsNoTracking()
            .AnyAsync(x =>
                x.MovieId == entity.MovieId &&
                x.EpisodeNumber == request.EpisodeNumber &&
                x.Id != id, ct);

        if (duplicateExists)
        {
            throw new ConflictException(
                "EPISODE_NUMBER_DUPLICATE",
                $"Episode number {request.EpisodeNumber} already exists for movie id {entity.MovieId}.");
        }

        entity.EpisodeNumber = request.EpisodeNumber;
        entity.Title = NormalizeTitle(request.Title);

        await _db.SaveChangesAsync(ct);
        var currentUser = _currentUserService.GetCurrentUser();

        await _auditService.EnqueueAsync(
            new AuditLogRequest(
                EventId: Guid.NewGuid().ToString("N"),
                ActorUserId: currentUser.UserId ?? 0,
                Action: "UPDATE",
                Entity: "Episode",
                EntityId: entity.Id,
                Payload: new
                {
                    entity.Id,
                    entity.MovieId,
                    entity.EpisodeNumber,
                    entity.Title
                }),
            ct);

        BackgroundJob.Enqueue<NotifyJob>(x =>
            x.ExecuteAsync(
                $"Episode updated: {entity.Title} (Id={entity.Id}, MovieId={entity.MovieId}, EpisodeNumber={entity.EpisodeNumber})"));
        return MapToItem(entity);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.Episodes
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
        {
            throw new NotFoundException(
                "EPISODE_NOT_FOUND",
                $"Episode with id {id} was not found.");
        }

        var payload = new
        {
            entity.Id,
            entity.MovieId,
            entity.EpisodeNumber,
            entity.Title
        };

        var currentUser = _currentUserService.GetCurrentUser();

        _db.Episodes.Attach(entity);
        _db.Episodes.Remove(entity);

        await _db.SaveChangesAsync(ct);

        await _auditService.EnqueueAsync(
            new AuditLogRequest(
                EventId: Guid.NewGuid().ToString("N"),
                ActorUserId: currentUser.UserId ?? 0,
                Action: "DELETE",
                Entity: "Episode",
                EntityId: entity.Id,
                Payload: payload),
            ct);

        BackgroundJob.Enqueue<NotifyJob>(x =>
            x.ExecuteAsync(
                $"Episode deleted: {payload.Title} (Id={payload.Id}, MovieId={payload.MovieId}, EpisodeNumber={payload.EpisodeNumber})"));
    }

    private static void ValidateEpisodeInput(int episodeNumber, string? title)
    {
        var errors = new Dictionary<string, string[]>();

        if (episodeNumber <= 0)
            errors["episodeNumber"] = new[] { "EpisodeNumber must be greater than 0." };

        if (string.IsNullOrWhiteSpace(title))
            errors["title"] = new[] { "Title is required." };
        else if (title.Trim().Length > 300)
            errors["title"] = new[] { "Title must not exceed 300 characters." };

        if (errors.Count > 0)
            throw new ValidationException("VALIDATION_ERROR", errors);
    }

    private static string NormalizeTitle(string? title)
        => (title ?? "").Trim();

    private static EpisodeItem MapToItem(Episode entity)
    {
        return new EpisodeItem(
            entity.Id,
            entity.EpisodeNumber,
            entity.Title,
            Array.Empty<VideoItem>()
        );
    }
}