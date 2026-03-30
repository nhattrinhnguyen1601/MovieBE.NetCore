using Hangfire;
using MovieApi.Application.DTOs.Audit;
using MovieApi.Application.Interfaces;

namespace MovieApi.Infrastructure.Services;

public sealed class AuditService : IAuditService
{
    public Task EnqueueAsync(AuditLogRequest request, CancellationToken ct = default)
    {
        BackgroundJob.Enqueue<AuditLogJob>(x => x.ExecuteAsync(request));
        return Task.CompletedTask;
    }
}