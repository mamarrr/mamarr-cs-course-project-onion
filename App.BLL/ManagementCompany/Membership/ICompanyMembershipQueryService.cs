namespace App.BLL.ManagementCompany.Membership;

public interface ICompanyMembershipQueryService
{
    Task<CompanyMembershipListResult> ListCompanyMembersAsync(
        CompanyAdminAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<CompanyMembershipEditResult> GetMembershipForEditAsync(
        CompanyAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default);
}

