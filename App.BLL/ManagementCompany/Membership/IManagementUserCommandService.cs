namespace App.BLL.ManagementCompany.Membership;

public interface IManagementUserCommandService
{
    Task<ManagementUserAddResult> AddUserByEmailAsync(
        ManagementUserAdminAuthorizedContext context,
        ManagementUserAddRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagementUserUpdateResult> UpdateMembershipAsync(
        ManagementUserAdminAuthorizedContext context,
        Guid membershipId,
        ManagementUserUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagementUserDeleteResult> DeleteMembershipAsync(
        ManagementUserAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default);
}

