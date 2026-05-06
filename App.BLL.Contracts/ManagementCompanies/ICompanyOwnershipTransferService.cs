using App.BLL.DTO.ManagementCompanies.Models;
using FluentResults;

namespace App.BLL.Contracts.ManagementCompanies;

public interface ICompanyOwnershipTransferService
{
    Task<Result<IReadOnlyList<OwnershipTransferCandidate>>> GetOwnershipTransferCandidatesAsync(
        CompanyAdminAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<Result<OwnershipTransferModel>> TransferOwnershipAsync(
        CompanyAdminAuthorizedContext context,
        TransferOwnershipRequest request,
        CancellationToken cancellationToken = default);
}

