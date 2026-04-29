using Base.DAL.Contracts;

namespace App.Contracts.DAL.ManagementCompanies;

public interface IManagementCompanyRepository : IBaseRepository<ManagementCompanyDalDto>
{
    Task<ManagementCompanyDalDto?> FirstBySlugAsync(
        string companySlug,
        CancellationToken cancellationToken = default);

    Task<string?> FindActiveUserRoleCodeAsync(
        Guid appUserId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> UserBelongsToCompanyAsync(
        Guid appUserId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);
}
