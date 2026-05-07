using App.DAL.DTO.Admin.Companies;

namespace App.DAL.Contracts.Repositories.Admin;

public interface IAdminCompanyRepository
{
    Task<IReadOnlyList<AdminCompanyListItemDalDto>> SearchCompaniesAsync(AdminCompanySearchDalDto search, CancellationToken cancellationToken = default);
    Task<AdminCompanyDetailsDalDto?> GetCompanyDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, Guid? exceptId = null, CancellationToken cancellationToken = default);
    Task<bool> RegistryCodeExistsAsync(string registryCode, Guid? exceptId = null, CancellationToken cancellationToken = default);
    Task<bool> UpdateCompanyAsync(Guid id, AdminCompanyUpdateDalDto dto, CancellationToken cancellationToken = default);
}
