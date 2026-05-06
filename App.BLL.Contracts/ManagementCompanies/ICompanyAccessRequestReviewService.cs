using App.BLL.DTO.ManagementCompanies.Models;
using FluentResults;

namespace App.BLL.Contracts.ManagementCompanies;

public interface ICompanyAccessRequestReviewService
{
    Task<Result<PendingAccessRequestListResult>> GetPendingAccessRequestsAsync(
        CompanyAdminAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<Result> ApprovePendingAccessRequestAsync(
        CompanyAdminAuthorizedContext context,
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<Result> RejectPendingAccessRequestAsync(
        CompanyAdminAuthorizedContext context,
        Guid requestId,
        CancellationToken cancellationToken = default);
}

