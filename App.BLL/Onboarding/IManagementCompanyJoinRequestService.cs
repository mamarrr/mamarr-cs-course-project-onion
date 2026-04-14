namespace App.BLL.Onboarding;

public interface IManagementCompanyJoinRequestService
{
    Task<CreateManagementCompanyJoinRequestResult> CreateJoinRequestAsync(
        CreateManagementCompanyJoinRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ManagementCompanyJoinRequestListItem>> ListPendingForCompanyAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<ResolveManagementCompanyJoinRequestResult> ApproveRequestAsync(
        Guid actorAppUserId,
        Guid managementCompanyId,
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<ResolveManagementCompanyJoinRequestResult> RejectRequestAsync(
        Guid actorAppUserId,
        Guid managementCompanyId,
        Guid requestId,
        CancellationToken cancellationToken = default);
}

