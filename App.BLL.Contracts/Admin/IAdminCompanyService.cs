using App.BLL.DTO.Admin.Companies;
using FluentResults;

namespace App.BLL.Contracts.Admin;

public interface IAdminCompanyService
{
    Task<AdminCompanyListDto> SearchCompaniesAsync(AdminCompanySearchDto search, CancellationToken cancellationToken = default);
    Task<AdminCompanyDetailsDto?> GetCompanyDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AdminCompanyEditDto?> GetCompanyForEditAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<AdminCompanyDetailsDto>> UpdateCompanyAsync(Guid id, AdminCompanyUpdateDto dto, CancellationToken cancellationToken = default);
}
