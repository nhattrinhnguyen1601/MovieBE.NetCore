namespace MovieApi.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public string EventId { get; set; } = default!; // unique idempotency key

    public long? ActorUserId { get; set; }
    public string Action { get; set; } = default!;
    public string Entity { get; set; } = default!;
    public long EntityId { get; set; } = default!;
    public string PayloadJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}