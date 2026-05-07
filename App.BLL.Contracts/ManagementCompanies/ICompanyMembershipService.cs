using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.ManagementCompanies.Commands;
using App.BLL.DTO.ManagementCompanies.Models;
using FluentResults;

namespace App.BLL.Contracts.ManagementCompanies;

public interface ICompanyMembershipService
{
    Task<Result<CompanyMembershipContext>> AuthorizeManagementAreaAccessAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<CompanyAdminAuthorizedContext>> AuthorizeAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<CompanyMembershipListResult>> ListCompanyMembersAsync(
        CompanyAdminAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<Result<CompanyMembershipEditModel>> GetMembershipForEditAsync(
        CompanyAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CompanyMembershipRoleOption>>> GetAddRoleOptionsAsync(
        CompanyAdminAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CompanyMembershipRoleOption>>> GetEditRoleOptionsAsync(
        CompanyAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CompanyMembershipRoleOption>>> GetAvailableRolesAsync(
        CancellationToken cancellationToken = default);

    Task<Result> CreateJoinRequestAsync(
        CreateCompanyJoinRequestCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<Guid>> AddUserByEmailAsync(
        CompanyAdminAuthorizedContext context,
        CompanyMembershipAddRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateMembershipAsync(
        CompanyAdminAuthorizedContext context,
        Guid membershipId,
        CompanyMembershipUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteMembershipAsync(
        CompanyAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<OwnershipTransferCandidate>>> GetOwnershipTransferCandidatesAsync(
        CompanyAdminAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<Result<OwnershipTransferModel>> TransferOwnershipAsync(
        CompanyAdminAuthorizedContext context,
        TransferOwnershipRequest request,
        CancellationToken cancellationToken = default);

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
