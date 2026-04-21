namespace App.BLL.Onboarding.CompanyJoinRequests;

public interface ICompanyJoinRequestService
{
    Task<CompanyJoinRequestResult> CreateJoinRequestAsync(
        CompanyJoinRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompanyJoinRequestListItem>> ListPendingForCompanyAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<ResolveCompanyJoinRequestResult> ApproveRequestAsync(
        Guid actorAppUserId,
        Guid managementCompanyId,
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<ResolveCompanyJoinRequestResult> RejectRequestAsync(
        Guid actorAppUserId,
        Guid managementCompanyId,
        Guid requestId,
        CancellationToken cancellationToken = default);
}

