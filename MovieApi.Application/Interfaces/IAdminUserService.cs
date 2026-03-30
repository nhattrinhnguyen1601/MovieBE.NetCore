using MovieApi.Application.Common.Models;
using MovieApi.Application.DTOs.AdminUsers;

namespace MovieApi.Application.Interfaces;

public interface IAdminUserService
{
    Task<PagedResult<AdminUserListItemResponse>> GetUsersAsync(AdminUserQuery query, CancellationToken ct = default);

    Task LockAsync(long id, CancellationToken ct = default);

    Task UnlockAsync(long id, CancellationToken ct = default);
}