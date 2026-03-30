using MovieApi.Application.DTOs.Audit;
namespace MovieApi.Application.Interfaces;

public interface IAuditService
{
    Task EnqueueAsync(AuditLogRequest request, CancellationToken ct);
}