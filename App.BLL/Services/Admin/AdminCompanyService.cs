using App.BLL.Contracts.Admin;
using App.BLL.DTO.Admin.Companies;
using App.BLL.DTO.Common.Errors;
using App.BLL.Mappers.Admin;
using App.DAL.Contracts;
using FluentResults;

namespace App.BLL.Services.Admin;

public class AdminCompanyService : IAdminCompanyService
{
    private readonly IAppUOW _uow;
    private readonly AdminCompanyBllMapper _mapper = new();

    public AdminCompanyService(IAppUOW uow)
    {
        _uow = uow;
    }

    public async Task<AdminCompanyListDto> SearchCompaniesAsync(AdminCompanySearchDto search, CancellationToken cancellationToken = default)
    {
        var companies = await _uow.AdminCompanies.SearchCompaniesAsync(_mapper.Map(search), cancellationToken);
        return new AdminCompanyListDto
        {
            Search = search,
            Companies = companies.Select(_mapper.Map).ToList()
        };
    }

    public async Task<AdminCompanyDetailsDto?> GetCompanyDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var company = await _uow.AdminCompanies.GetCompanyDetailsAsync(id, cancellationToken);
        return company is null ? null : _mapper.Map(company);
    }

    public async Task<AdminCompanyEditDto?> GetCompanyForEditAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var company = await GetCompanyDetailsAsync(id, cancellationToken);
        return company is null
            ? null
            : new AdminCompanyEditDto
            {
                Id = company.Id,
                Name = company.Name,
                RegistryCode = company.RegistryCode,
                VatNumber = company.VatNumber,
                Email = company.Email,
                Phone = company.Phone,
                Address = company.Address,
                Slug = company.Slug
            };
    }

    public async Task<Result<AdminCompanyDetailsDto>> UpdateCompanyAsync(Guid id, AdminCompanyUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateUpdateAsync(id, dto, cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<AdminCompanyDetailsDto>(validation.Errors);
        }

        if (!await _uow.AdminCompanies.UpdateCompanyAsync(id, _mapper.Map(dto), cancellationToken))
        {
            return Result.Fail<AdminCompanyDetailsDto>(new NotFoundError("Management company was not found."));
        }

        await _uow.SaveChangesAsync(cancellationToken);
        var updated = await _uow.AdminCompanies.GetCompanyDetailsAsync(id, cancellationToken);
        return updated is null
            ? Result.Fail<AdminCompanyDetailsDto>(new NotFoundError("Management company was not found."))
            : Result.Ok(_mapper.Map(updated));
    }

    private async Task<Result> ValidateUpdateAsync(Guid id, AdminCompanyUpdateDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) ||
            string.IsNullOrWhiteSpace(dto.RegistryCode) ||
            string.IsNullOrWhiteSpace(dto.VatNumber) ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Phone) ||
            string.IsNullOrWhiteSpace(dto.Address) ||
            string.IsNullOrWhiteSpace(dto.Slug))
        {
            return Result.Fail(new ValidationAppError("Required fields are missing.", []));
        }

        if (await _uow.AdminCompanies.SlugExistsAsync(dto.Slug, id, cancellationToken))
        {
            return Result.Fail(new ConflictError("Management company with this slug already exists."));
        }

        if (await _uow.AdminCompanies.RegistryCodeExistsAsync(dto.RegistryCode, id, cancellationToken))
        {
            return Result.Fail(new ConflictError("Management company with this registry code already exists."));
        }

        return Result.Ok();
    }
}
