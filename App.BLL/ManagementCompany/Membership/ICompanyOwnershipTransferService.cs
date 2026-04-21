namespace App.BLL.ManagementCompany.Membership;

public interface ICompanyOwnershipTransferService
{
    Task<OwnershipTransferCandidateListResult> GetOwnershipTransferCandidatesAsync(
        CompanyAdminAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<OwnershipTransferResult> TransferOwnershipAsync(
        CompanyAdminAuthorizedContext context,
        TransferOwnershipRequest request,
        CancellationToken cancellationToken = default);
}

