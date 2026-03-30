using Hangfire.Dashboard;

namespace MovieApi.Api.Authorization;

public sealed class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly IHostEnvironment _env;

    public HangfireAuthorizationFilter(IHostEnvironment env)
    {
        _env = env;
    }

    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();

        if (_env.IsDevelopment())
            return true;

        return http.User.Identity?.IsAuthenticated == true &&
               http.User.IsInRole("Admin");
    }
}