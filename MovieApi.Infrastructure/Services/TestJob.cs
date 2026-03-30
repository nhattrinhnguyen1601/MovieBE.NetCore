using Microsoft.Extensions.Logging;

namespace MovieApi.Infrastructure.Services;

public sealed class TestJob
{
    private readonly ILogger<TestJob> _logger;

    public TestJob(ILogger<TestJob> logger)
    {
        _logger = logger;
    }

    public Task RunAsync()
    {
        _logger.LogInformation("Hangfire TestJob executed at {TimeUtc}", DateTime.UtcNow);
        Console.WriteLine($"[Hangfire] TestJob executed at {DateTime.UtcNow:O}");
        return Task.CompletedTask;
    }
}