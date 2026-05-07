using App.DAL.DTO.Admin.Users;

namespace App.DAL.Contracts.Repositories.Admin;

public interface IAdminUserRepository
{
    Task<IReadOnlyList<AdminUserListItemDalDto>> SearchUsersAsync(AdminUserSearchDalDto search, CancellationToken cancellationToken = default);
    Task<AdminUserDetailsDalDto?> GetUserDetailsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasSystemAdminRoleAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> CountSystemAdminsAsync(CancellationToken cancellationToken = default);
    Task<bool> SetLockoutEndAsync(Guid userId, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken = default);
}
