using App.Domain;

namespace App.BLL.Management;

public interface IManagementUserRoleService
{
    Task<IReadOnlyList<ManagementRoleOption>> GetAddRoleOptionsAsync(
        ManagementUserAdminAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<ManagementRoleOptionsResult> GetEditRoleOptionsAsync(
        ManagementUserAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ManagementCompanyRole>> GetAvailableRolesAsync(
        CancellationToken cancellationToken = default);
}

