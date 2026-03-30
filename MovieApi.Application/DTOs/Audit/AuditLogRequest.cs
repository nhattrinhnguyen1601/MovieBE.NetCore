namespace MovieApi.Application.DTOs.Audit;

public sealed record AuditLogRequest(
    string EventId,
    long? ActorUserId,
    string Action,
    string Entity,
    long EntityId,
    object Payload
);