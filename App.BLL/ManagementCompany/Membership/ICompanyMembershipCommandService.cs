namespace App.BLL.ManagementCompany.Membership;

public interface ICompanyMembershipCommandService
{
    Task<CompanyMembershipAddResult> AddUserByEmailAsync(
        CompanyAdminAuthorizedContext context,
        CompanyMembershipAddRequest request,
        CancellationToken cancellationToken = default);

    Task<CompanyMembershipUpdateResult> UpdateMembershipAsync(
        CompanyAdminAuthorizedContext context,
        Guid membershipId,
        CompanyMembershipUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<CompanyMembershipDeleteResult> DeleteMembershipAsync(
        CompanyAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default);
}

