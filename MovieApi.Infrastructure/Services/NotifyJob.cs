using Microsoft.Extensions.Logging;

namespace MovieApi.Infrastructure.Services;

public sealed class NotifyJob
{
    private readonly ILogger<NotifyJob> _logger;

    public NotifyJob(ILogger<NotifyJob> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(string message)
    {
        _logger.LogInformation("[NotifyJob] {Message} at {TimeUtc}", message, DateTime.UtcNow);

        Console.WriteLine($"[Notify] {message} at {DateTime.UtcNow:O}");

        return Task.CompletedTask;
    }
}