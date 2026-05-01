using App.BLL.Contracts.ManagementCompanies.Models;
using FluentResults;

namespace App.BLL.Contracts.ManagementCompanies.Services;

public interface ICompanyMembershipCommandService
{
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
}

