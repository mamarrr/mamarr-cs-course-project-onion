using App.BLL.Contracts.ManagementCompanies.Models;

namespace App.BLL.Contracts.ManagementCompanies.Services;

public interface ICompanyAccessRequestReviewService
{
    Task<PendingAccessRequestListResult> GetPendingAccessRequestsAsync(
        CompanyAdminAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<PendingAccessRequestActionResult> ApprovePendingAccessRequestAsync(
        CompanyAdminAuthorizedContext context,
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<PendingAccessRequestActionResult> RejectPendingAccessRequestAsync(
        CompanyAdminAuthorizedContext context,
        Guid requestId,
        CancellationToken cancellationToken = default);
}

