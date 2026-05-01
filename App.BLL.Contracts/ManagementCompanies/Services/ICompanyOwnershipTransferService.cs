using App.BLL.Contracts.ManagementCompanies.Models;
using FluentResults;

namespace App.BLL.Contracts.ManagementCompanies.Services;

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

