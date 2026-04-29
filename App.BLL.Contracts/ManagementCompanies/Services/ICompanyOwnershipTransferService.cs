using App.BLL.Contracts.ManagementCompanies.Models;

namespace App.BLL.Contracts.ManagementCompanies.Services;

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

