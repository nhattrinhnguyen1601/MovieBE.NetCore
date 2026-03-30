using Microsoft.EntityFrameworkCore;
using MovieApi.Application.Common.Exceptions;
using MovieApi.Application.Common.Models;
using MovieApi.Application.DTOs.AdminUsers;
using MovieApi.Application.Interfaces;
using MovieApi.Domain.Enums;
using MovieApi.Infrastructure.Persistence;

namespace MovieApi.Infrastructure.Services;

public sealed class AdminUserService : IAdminUserService
{
    private readonly AppDbContext _db;

    public AdminUserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AdminUserListItemResponse>> GetUsersAsync(AdminUserQuery query, CancellationToken ct = default)
    {
        var usersQuery = _db.Users
            .AsNoTracking()
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .AsQueryable();

        var totalItems = await usersQuery.CountAsync(ct);

        var items = await usersQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new AdminUserListItemResponse(
                x.Id,
                x.Email,
                x.Status.ToString(),
                x.UserRoles
                    .Where(ur => ur.Role != null)
                    .Select(ur => ur.Role.Name)
                    .Distinct()
                    .ToArray(),
                x.CreatedAt
            ))
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(totalItems / (double)query.PageSize);

        return new PagedResult<AdminUserListItemResponse>(
            items,
            query.Page,
            query.PageSize,
            totalItems,
            totalPages
        );
    }

    public async Task LockAsync(long id, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (user is null)
        {
            throw new NotFoundException(
                "USER_NOT_FOUND",
                $"User with id {id} was not found.");
        }

        user.Status = UserStatus.Locked;
        await _db.SaveChangesAsync(ct);
    }

    public async Task UnlockAsync(long id, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (user is null)
        {
            throw new NotFoundException(
                "USER_NOT_FOUND",
                $"User with id {id} was not found.");
        }

        user.Status = UserStatus.Active;
        await _db.SaveChangesAsync(ct);
    }
}