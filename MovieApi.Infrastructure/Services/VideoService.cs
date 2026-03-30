using Microsoft.EntityFrameworkCore;
using MovieApi.Application.Common.Exceptions;
using MovieApi.Application.DTOs.Movies;
using MovieApi.Application.DTOs.Videos;
using MovieApi.Application.Interfaces;
using MovieApi.Domain.Entities;
using MovieApi.Infrastructure.Persistence;
using Hangfire;
using MovieApi.Application.DTOs.Audit;
namespace MovieApi.Infrastructure.Services;

public sealed class VideoService : IVideoService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public VideoService(AppDbContext db, IAuditService auditService, ICurrentUserService currentUserService)
    {
        _db = db;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<VideoItem> CreateAsync(long episodeId, VideoCreateRequest request, CancellationToken ct = default)
    {
        ValidateVideoInput(request.ServerName, request.Quality, request.Url);

        var episodeExists = await _db.Episodes
            .AsNoTracking()
            .AnyAsync(x => x.Id == episodeId, ct);

        if (!episodeExists)
        {
            throw new NotFoundException(
                "EPISODE_NOT_FOUND",
                $"Episode with id {episodeId} was not found.");
        }

        var shouldBeDefault = request.IsDefault;

        // Nếu episode chưa có video nào thì video đầu tiên tự động là default
        var hasAnyVideo = await _db.Videos
            .AsNoTracking()
            .AnyAsync(x => x.EpisodeId == episodeId, ct);

        if (!hasAnyVideo)
        {
            shouldBeDefault = true;
        }

        if (shouldBeDefault)
        {
            var existingDefaults = await _db.Videos
                .Where(x => x.EpisodeId == episodeId && x.IsDefault)
                .ToListAsync(ct);

            foreach (var item in existingDefaults)
            {
                item.IsDefault = false;
            }
        }

        var entity = new Video
        {
            EpisodeId = episodeId,
            ServerName = NormalizeServerName(request.ServerName),
            Quality = NormalizeQuality(request.Quality),
            Url = NormalizeUrl(request.Url),
            IsDefault = shouldBeDefault,
            CreatedAt = DateTime.UtcNow
        };

        _db.Videos.Add(entity);
        await _db.SaveChangesAsync(ct);
        var currentUser = _currentUserService.GetCurrentUser();

        await _auditService.EnqueueAsync(
            new AuditLogRequest(
                EventId: Guid.NewGuid().ToString("N"),
                ActorUserId: currentUser.UserId ?? 0,
                Action: "CREATE",
                Entity: "Video",
                EntityId: entity.Id,
                Payload: new
                {
                    entity.Id,
                    entity.EpisodeId,
                    entity.ServerName,
                    entity.Quality,
                    entity.Url,
                    entity.IsDefault
                }),
            ct);

        BackgroundJob.Enqueue<NotifyJob>(x =>
            x.ExecuteAsync(
                $"Video created: {entity.ServerName} {entity.Quality} (Id={entity.Id}, EpisodeId={entity.EpisodeId}, IsDefault={entity.IsDefault})"));

        return MapToItem(entity);
    }

    public async Task<VideoItem> SetDefaultAsync(long id, SetDefaultVideoRequest request, CancellationToken ct = default)
    {
        if (!request.IsDefault)
        {
            throw new ValidationException(
                "VALIDATION_ERROR",
                new Dictionary<string, string[]>
                {
                    ["isDefault"] = new[] { "Only setting isDefault = true is supported for this endpoint." }
                });
        }

        var entity = await _db.Videos
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
        {
            throw new NotFoundException(
                "VIDEO_NOT_FOUND",
                $"Video with id {id} was not found.");
        }

        var siblingVideos = await _db.Videos
            .Where(x => x.EpisodeId == entity.EpisodeId)
            .ToListAsync(ct);

        foreach (var item in siblingVideos)
        {
            item.IsDefault = item.Id == entity.Id;
        }

        await _db.SaveChangesAsync(ct);
        var currentUser = _currentUserService.GetCurrentUser();

        await _auditService.EnqueueAsync(
            new AuditLogRequest(
                EventId: Guid.NewGuid().ToString("N"),
                ActorUserId: currentUser.UserId ?? 0,
                Action: "SET_DEFAULT",
                Entity: "Video",
                EntityId: entity.Id,
                Payload: new
                {
                    entity.Id,
                    entity.EpisodeId,
                    entity.ServerName,
                    entity.Quality,
                    entity.Url,
                    entity.IsDefault
                }),
            ct);

        BackgroundJob.Enqueue<NotifyJob>(x =>
            x.ExecuteAsync(
                $"Video set default: {entity.ServerName} {entity.Quality} (Id={entity.Id}, EpisodeId={entity.EpisodeId})"));


        return MapToItem(entity);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.Videos
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
        {
            throw new NotFoundException(
                "VIDEO_NOT_FOUND",
                $"Video with id {id} was not found.");
        }

        var episodeId = entity.EpisodeId;
        var wasDefault = entity.IsDefault;

        var payload = new
        {
            entity.Id,
            entity.EpisodeId,
            entity.ServerName,
            entity.Quality,
            entity.Url,
            entity.IsDefault
        };

        var currentUser = _currentUserService.GetCurrentUser();

        _db.Videos.Attach(entity);
        _db.Videos.Remove(entity);

        await _db.SaveChangesAsync(ct);

        if (wasDefault)
        {
            var replacement = await _db.Videos
                .Where(x => x.EpisodeId == episodeId)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(ct);

            if (replacement is not null && !replacement.IsDefault)
            {
                replacement.IsDefault = true;
                await _db.SaveChangesAsync(ct);
            }
        }

        await _auditService.EnqueueAsync(
            new AuditLogRequest(
                EventId: Guid.NewGuid().ToString("N"),
                ActorUserId: currentUser.UserId ?? 0,
                Action: "DELETE",
                Entity: "Video",
                EntityId: payload.Id,
                Payload: payload),
            ct);

        BackgroundJob.Enqueue<NotifyJob>(x =>
            x.ExecuteAsync(
                $"Video deleted: {payload.ServerName} {payload.Quality} (Id={payload.Id}, EpisodeId={payload.EpisodeId})"));

    }

    private static void ValidateVideoInput(string? serverName, string? quality, string? url)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(serverName))
            errors["serverName"] = new[] { "ServerName is required." };
        else if (serverName.Trim().Length > 50)
            errors["serverName"] = new[] { "ServerName must not exceed 50 characters." };

        if (string.IsNullOrWhiteSpace(quality))
            errors["quality"] = new[] { "Quality is required." };
        else if (quality.Trim().Length > 20)
            errors["quality"] = new[] { "Quality must not exceed 20 characters." };

        if (string.IsNullOrWhiteSpace(url))
            errors["url"] = new[] { "Url is required." };
        else if (url.Trim().Length > 2000)
            errors["url"] = new[] { "Url must not exceed 2000 characters." };
        else if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out _))
            errors["url"] = new[] { "Url must be a valid absolute URL." };

        if (errors.Count > 0)
            throw new ValidationException("VALIDATION_ERROR", errors);
    }

    private static string NormalizeServerName(string? serverName)
        => (serverName ?? "").Trim();

    private static string NormalizeQuality(string? quality)
        => (quality ?? "").Trim();

    private static string NormalizeUrl(string? url)
        => (url ?? "").Trim();

    private static VideoItem MapToItem(Video entity)
    {
        return new VideoItem(
            entity.Id,
            entity.ServerName,
            entity.Quality,
            entity.Url,
            entity.IsDefault
        );
    }
}