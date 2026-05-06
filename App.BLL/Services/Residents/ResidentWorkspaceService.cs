using App.BLL.Contracts.Common;
using App.BLL.Contracts.Residents;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Residents.Commands;
using App.BLL.DTO.Residents.Errors;
using App.BLL.DTO.Residents.Models;
using App.BLL.DTO.Residents.Queries;
using App.BLL.Mappers.Residents;
using App.DAL.Contracts;
using App.DAL.DTO.Residents;
using FluentResults;

namespace App.BLL.Services.Residents;

public class ResidentWorkspaceService : IResidentWorkspaceService
{
    private readonly IResidentAccessService _residentAccessService;
    private readonly IAppUOW _uow;

    public ResidentWorkspaceService(
        IResidentAccessService residentAccessService,
        IAppUOW uow)
    {
        _residentAccessService = residentAccessService;
        _uow = uow;
    }

    public async Task<Result<ResidentDashboardModel>> GetDashboardAsync(
        GetResidentProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _residentAccessService.ResolveResidentWorkspaceAsync(query, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        return Result.Ok(new ResidentDashboardModel
        {
            Workspace = workspace.Value
        });
    }

    public async Task<Result<CompanyResidentsModel>> GetResidentsAsync(
        GetResidentsQuery query,
        CancellationToken cancellationToken = default)
    {
        var company = await _residentAccessService.ResolveCompanyResidentsAsync(query, cancellationToken);
        if (company.IsFailed)
        {
            return Result.Fail(company.Errors);
        }

        var residents = await _uow.Residents.AllByCompanyAsync(
            company.Value.ManagementCompanyId,
            cancellationToken);

        return Result.Ok(new CompanyResidentsModel
        {
            AppUserId = company.Value.AppUserId,
            ManagementCompanyId = company.Value.ManagementCompanyId,
            CompanySlug = company.Value.CompanySlug,
            CompanyName = company.Value.CompanyName,
            Residents = residents.Select(ResidentBllMapper.MapListItem).ToList()
        });
    }

    public async Task<Result<ResidentProfileModel>> CreateAsync(
        CreateResidentCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateCreate(command);
        if (validation.IsFailed)
        {
            return Result.Fail(validation.Errors);
        }

        var company = await _residentAccessService.ResolveCompanyResidentsAsync(
            new GetResidentsQuery
            {
                UserId = command.UserId,
                CompanySlug = command.CompanySlug
            },
            cancellationToken);
        if (company.IsFailed)
        {
            return Result.Fail(company.Errors);
        }

        var normalizedFirstName = command.FirstName.Trim();
        var normalizedLastName = command.LastName.Trim();
        var normalizedIdCode = command.IdCode.Trim();
        var normalizedPreferredLanguage = string.IsNullOrWhiteSpace(command.PreferredLanguage)
            ? null
            : command.PreferredLanguage.Trim();

        var duplicateIdCode = await _uow.Residents.IdCodeExistsForCompanyAsync(
            company.Value.ManagementCompanyId,
            normalizedIdCode,
            cancellationToken: cancellationToken);
        if (duplicateIdCode)
        {
            return Result.Fail(new DuplicateResidentIdCodeError(
                App.Resources.Views.UiText.ResourceManager.GetString("ResidentIdCodeAlreadyExists")
                ?? "Resident with this ID code already exists in this company.",
                nameof(command.IdCode)));
        }

        var residentId = _uow.Residents.Add(new ResidentDalDto
        {
            ManagementCompanyId = company.Value.ManagementCompanyId,
            FirstName = normalizedFirstName,
            LastName = normalizedLastName,
            IdCode = normalizedIdCode,
            PreferredLanguage = normalizedPreferredLanguage
        });

        await _uow.SaveChangesAsync(cancellationToken);

        var profile = await _uow.Residents.FindProfileAsync(
            residentId,
            company.Value.ManagementCompanyId,
            cancellationToken);

        return profile is null
            ? Result.Fail(new NotFoundError("Resident profile was not found."))
            : Result.Ok(ResidentBllMapper.MapProfile(profile));
    }

    private static Result ValidateCreate(CreateResidentCommand command)
    {
        var failures = new List<ValidationFailureModel>();

        if (string.IsNullOrWhiteSpace(command.FirstName))
        {
            failures.Add(CreateRequiredFailure(nameof(command.FirstName), App.Resources.Views.UiText.FirstName));
        }

        if (string.IsNullOrWhiteSpace(command.LastName))
        {
            failures.Add(CreateRequiredFailure(nameof(command.LastName), App.Resources.Views.UiText.LastName));
        }

        if (string.IsNullOrWhiteSpace(command.IdCode))
        {
            failures.Add(CreateRequiredFailure(nameof(command.IdCode), App.Resources.Views.UiText.IdCode));
        }

        return failures.Count == 0
            ? Result.Ok()
            : Result.Fail(new ResidentValidationError("Validation failed.", failures));
    }

    private static ValidationFailureModel CreateRequiredFailure(string propertyName, string displayName)
    {
        return new ValidationFailureModel
        {
            PropertyName = propertyName,
            ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", displayName)
        };
    }
}
