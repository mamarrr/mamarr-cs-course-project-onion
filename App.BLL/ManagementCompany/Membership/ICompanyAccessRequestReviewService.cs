namespace App.BLL.ManagementCompany.Membership;

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

