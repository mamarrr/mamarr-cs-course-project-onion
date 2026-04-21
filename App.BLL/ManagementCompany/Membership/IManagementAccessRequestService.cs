namespace App.BLL.ManagementCompany.Membership;

public interface IManagementAccessRequestService
{
    Task<PendingAccessRequestListResult> GetPendingAccessRequestsAsync(
        ManagementUserAdminAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<PendingAccessRequestActionResult> ApprovePendingAccessRequestAsync(
        ManagementUserAdminAuthorizedContext context,
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<PendingAccessRequestActionResult> RejectPendingAccessRequestAsync(
        ManagementUserAdminAuthorizedContext context,
        Guid requestId,
        CancellationToken cancellationToken = default);
}

