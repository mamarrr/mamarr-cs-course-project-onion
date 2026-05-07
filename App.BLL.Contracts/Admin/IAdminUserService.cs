using App.BLL.DTO.Admin.Users;
using FluentResults;

namespace App.BLL.Contracts.Admin;

public interface IAdminUserService
{
    Task<AdminUserListDto> SearchUsersAsync(AdminUserSearchDto search, CancellationToken cancellationToken = default);
    Task<AdminUserDetailsDto?> GetUserDetailsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<AdminUserDetailsDto>> LockUserAsync(Guid userId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<Result<AdminUserDetailsDto>> UnlockUserAsync(Guid userId, Guid actorUserId, CancellationToken cancellationToken = default);
}
