namespace App.BLL.ManagementCompany.Membership;

public interface IManagementOwnershipTransferService
{
    Task<OwnershipTransferCandidateListResult> GetOwnershipTransferCandidatesAsync(
        ManagementUserAdminAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<OwnershipTransferResult> TransferOwnershipAsync(
        ManagementUserAdminAuthorizedContext context,
        TransferOwnershipRequest request,
        CancellationToken cancellationToken = default);
}

