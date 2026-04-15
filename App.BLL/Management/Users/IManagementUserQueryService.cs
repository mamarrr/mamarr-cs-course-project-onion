namespace App.BLL.Management;

public interface IManagementUserQueryService
{
    Task<ManagementUserListResult> ListCompanyMembersAsync(
        ManagementUserAdminAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<ManagementUserEditResult> GetMembershipForEditAsync(
        ManagementUserAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default);
}

