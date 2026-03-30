using Hangfire;
using Microsoft.AspNetCore.Mvc;
using MovieApi.Infrastructure.Services;

namespace MovieApi.Api.Controllers;

[ApiController]
[Route("hangfire-test")]
public sealed class HangfireTestController : ControllerBase
{
    [HttpPost("enqueue")]
    public IActionResult Enqueue()
    {
        var jobId = BackgroundJob.Enqueue<TestJob>(x => x.RunAsync());
        return Ok(new { jobId });
    }
}