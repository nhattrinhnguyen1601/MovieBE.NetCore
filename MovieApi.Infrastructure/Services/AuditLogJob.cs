using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MovieApi.Application.DTOs.Audit;
using MovieApi.Infrastructure.Persistence;
using MovieApi.Domain.Entities;

namespace MovieApi.Infrastructure.Services;

public sealed class AuditLogJob
{
    private readonly AppDbContext _db;
    private readonly ILogger<AuditLogJob> _logger;

    public AuditLogJob(AppDbContext db, ILogger<AuditLogJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync(AuditLogRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EventId))
            throw new ArgumentException("EventId must not be empty.", nameof(request));

        var exists = await _db.AuditLogs
            .AsNoTracking()
            .AnyAsync(x => x.EventId == request.EventId);

        if (exists)
        {
            _logger.LogInformation(
                "AuditLogJob skipped because EventId {EventId} already exists.",
                request.EventId);
            return;
        }

        var entity = new AuditLog
        {
            EventId = request.EventId,
            ActorUserId = request.ActorUserId,
            Action = request.Action,
            Entity = request.Entity,
            EntityId = request.EntityId,
            PayloadJson = JsonSerializer.Serialize(request.Payload),
            CreatedAt = DateTime.UtcNow
        };

        _db.AuditLogs.Add(entity);

        try
        {
            await _db.SaveChangesAsync();
            _logger.LogInformation(
                "AuditLogJob inserted audit log for EventId {EventId}.",
                request.EventId);
        }
        catch (DbUpdateException ex)
        {
            // phòng trường hợp race condition / retry song song
            var duplicate = await _db.AuditLogs
                .AsNoTracking()
                .AnyAsync(x => x.EventId == request.EventId);

            if (duplicate)
            {
                _logger.LogWarning(
                    ex,
                    "AuditLogJob detected duplicate EventId {EventId} during save. Ignored.",
                    request.EventId);
                return;
            }

            throw;
        }
    }
}