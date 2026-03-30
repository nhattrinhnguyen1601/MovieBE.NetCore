using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MovieApi.Application.Common.Settings;
using MovieApi.Domain.Entities;
using MovieApi.Domain.Enums;

namespace MovieApi.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jwt = scope.ServiceProvider.GetRequiredService<IOptions<JwtSettings>>().Value;

        // đảm bảo DB đã migrate
        await db.Database.MigrateAsync(ct);

        // --- Seed Admin ---
        var email = (jwt.SeedAdminEmail ?? "").Trim().ToLowerInvariant();
        var password = jwt.SeedAdminPassword ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        var adminRoleId = 1L;
        var existingUser = await db.Users
            .Include(x => x.UserRoles)
            .FirstOrDefaultAsync(x => x.Email == email, ct);

        if (existingUser is null)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User
            {
                Email = email,
                PasswordHash = hash,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            user.UserRoles.Add(new UserRole
            {
                RoleId = adminRoleId
            });

            db.Users.Add(user);
            await db.SaveChangesAsync(ct);

            return;
        }

        // nếu user đã tồn tại nhưng chưa có role Admin → gán thêm
        var hasAdminRole = existingUser.UserRoles.Any(x => x.RoleId == adminRoleId);
        if (!hasAdminRole)
        {
            existingUser.UserRoles.Add(new UserRole
            {
                UserId = existingUser.Id,
                RoleId = adminRoleId
            });

            await db.SaveChangesAsync(ct);
        }
    }
}