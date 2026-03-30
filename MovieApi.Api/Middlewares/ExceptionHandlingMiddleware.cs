using System.Text.Json;
using MovieApi.Application.Common.Exceptions;

namespace MovieApi.Api.Middlewares;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var response = new
            {
                traceId = context.TraceIdentifier,
                code = ex.Code,
                message = "Validation failed.",
                details = ex.Errors
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (NotFoundException ex)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/json";

            var response = new
            {
                traceId = context.TraceIdentifier,
                code = ex.Code,
                message = ex.Message,
                details = (object?)null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (ConflictException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var response = new
            {
                traceId = context.TraceIdentifier,
                code = ex.Code,
                message = ex.Message,
                details = (object?)null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred.");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new
            {
                traceId = context.TraceIdentifier,
                code = "INTERNAL_SERVER_ERROR",
                message = "An unexpected error occurred.",
                details = (object?)null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }

}