using App.BLL.Contracts.Common.Deletion;
using App.BLL.Contracts.Residents;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Residents;
using App.BLL.DTO.Residents.Errors;
using App.BLL.DTO.Residents.Models;
using App.BLL.Mappers.Residents;
using App.DAL.Contracts;
using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Residents;
using Base.BLL;
using FluentResults;

namespace App.BLL.Services.Residents;

public class ResidentService :
    BaseService<ResidentBllDto, ResidentDalDto, IResidentRepository, IAppUOW>,
    IResidentService
{
    private static readonly HashSet<string> DeleteAllowedRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER"
    };

    private static readonly HashSet<string> AccessAllowedRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER",
        "FINANCE",
        "SUPPORT"
    };

    private readonly IAppDeleteGuard _deleteGuard;

    public ResidentService(
        IAppUOW uow,
        IAppDeleteGuard deleteGuard)
        : base(uow.Residents, uow, new ResidentBllDtoMapper())
    {
        _deleteGuard = deleteGuard;
    }

    public async Task<Result<CompanyResidentsModel>> ResolveCompanyResidentsAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default)
    {
        if (route.AppUserId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authentication is required."));
        }

        if (string.IsNullOrWhiteSpace(route.CompanySlug))
        {
            return Result.Fail(new NotFoundError(App.Resources.Views.UiText.ManagementCompanyWasNotFound));
        }

        var company = await ServiceUOW.ManagementCompanies.FirstBySlugAsync(
            route.CompanySlug,
            cancellationToken);
        if (company is null)
        {
            return Result.Fail(new NotFoundError(App.Resources.Views.UiText.ManagementCompanyWasNotFound));
        }

        var roleCode = await ServiceUOW.ManagementCompanies.FindActiveUserRoleCodeAsync(
            route.AppUserId,
            company.Id,
            cancellationToken);

        if (roleCode is null || !AccessAllowedRoleCodes.Contains(roleCode))
        {
            return Result.Fail(new ForbiddenError("Access denied."));
        }

        return Result.Ok(new CompanyResidentsModel
        {
            AppUserId = route.AppUserId,
            ManagementCompanyId = company.Id,
            CompanySlug = company.Slug,
            CompanyName = company.Name
        });
    }

    public async Task<Result<ResidentWorkspaceModel>> ResolveWorkspaceAsync(
        ResidentRoute route,
        CancellationToken cancellationToken = default)
    {
        if (route.AppUserId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authentication is required."));
        }

        if (string.IsNullOrWhiteSpace(route.CompanySlug))
        {
            return Result.Fail(new NotFoundError("Resident context was not found."));
        }

        var company = await ServiceUOW.ManagementCompanies.FirstBySlugAsync(
            route.CompanySlug,
            cancellationToken);
        if (company is null)
        {
            return Result.Fail(new NotFoundError("Resident context was not found."));
        }

        if (string.IsNullOrWhiteSpace(route.ResidentIdCode))
        {
            return Result.Fail(new NotFoundError("Resident context was not found."));
        }

        var resident = await ServiceUOW.Residents.FirstProfileAsync(
            route.CompanySlug,
            route.ResidentIdCode,
            cancellationToken);

        if (resident is null || resident.ManagementCompanyId != company.Id)
        {
            return Result.Fail(new NotFoundError("Resident context was not found."));
        }

        var roleCode = await ServiceUOW.ManagementCompanies.FindActiveUserRoleCodeAsync(
            route.AppUserId,
            company.Id,
            cancellationToken);
        if (roleCode is not null && AccessAllowedRoleCodes.Contains(roleCode))
        {
            return Result.Ok(new ResidentWorkspaceModel
            {
                AppUserId = route.AppUserId,
                ManagementCompanyId = resident.ManagementCompanyId,
                CompanySlug = resident.CompanySlug,
                CompanyName = resident.CompanyName,
                ResidentId = resident.Id,
                ResidentIdCode = resident.IdCode,
                FirstName = resident.FirstName,
                LastName = resident.LastName,
                FullName = BuildFullName(resident.FirstName, resident.LastName),
                PreferredLanguage = resident.PreferredLanguage
            });
        }

        var hasResidentContext = await ServiceUOW.Residents.HasActiveUserResidentContextAsync(
            route.AppUserId,
            resident.Id,
            cancellationToken);

        return hasResidentContext
            ? Result.Ok(new ResidentWorkspaceModel
            {
                AppUserId = route.AppUserId,
                ManagementCompanyId = resident.ManagementCompanyId,
                CompanySlug = resident.CompanySlug,
                CompanyName = resident.CompanyName,
                ResidentId = resident.Id,
                ResidentIdCode = resident.IdCode,
                FirstName = resident.FirstName,
                LastName = resident.LastName,
                FullName = BuildFullName(resident.FirstName, resident.LastName),
                PreferredLanguage = resident.PreferredLanguage
            })
            : Result.Fail(new ForbiddenError("Access denied."));
    }

    public async Task<Result<CompanyResidentsModel>> ListForCompanyAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default)
    {
        var company = await ResolveCompanyResidentsAsync(route, cancellationToken);
        if (company.IsFailed)
        {
            return Result.Fail(company.Errors);
        }

        var residents = await ServiceUOW.Residents.AllByCompanyAsync(
            company.Value.ManagementCompanyId,
            cancellationToken);

        return Result.Ok(new CompanyResidentsModel
        {
            AppUserId = company.Value.AppUserId,
            ManagementCompanyId = company.Value.ManagementCompanyId,
            CompanySlug = company.Value.CompanySlug,
            CompanyName = company.Value.CompanyName,
            Residents = residents.Select(resident => new ResidentListItemModel
            {
                ResidentId = resident.Id,
                FirstName = resident.FirstName,
                LastName = resident.LastName,
                FullName = BuildFullName(resident.FirstName, resident.LastName),
                IdCode = resident.IdCode,
                PreferredLanguage = resident.PreferredLanguage
            }).ToList()
        });
    }

    public async Task<Result<ResidentDashboardModel>> GetDashboardAsync(
        ResidentRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveWorkspaceAsync(route, cancellationToken);
        return workspace.IsFailed
            ? Result.Fail<ResidentDashboardModel>(workspace.Errors)
            : Result.Ok(new ResidentDashboardModel { Workspace = workspace.Value });
    }

    public async Task<Result<ResidentProfileModel>> GetProfileAsync(
        ResidentRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveWorkspaceAsync(route, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        return await GetProfileAsync(workspace.Value, cancellationToken);
    }

    public async Task<Result<ResidentBllDto>> CreateAsync(
        ManagementCompanyRoute route,
        ResidentBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var company = await ResolveCompanyResidentsAsync(route, cancellationToken);
        if (company.IsFailed)
        {
            return Result.Fail(company.Errors);
        }

        var validation = Validate(dto);
        if (validation.IsFailed)
        {
            return Result.Fail<ResidentBllDto>(validation.Errors);
        }

        var normalized = Normalize(dto);
        var duplicateIdCode = await ServiceUOW.Residents.IdCodeExistsForCompanyAsync(
            company.Value.ManagementCompanyId,
            normalized.IdCode,
            cancellationToken: cancellationToken);
        if (duplicateIdCode)
        {
            return Result.Fail(new DuplicateResidentIdCodeError(
                App.Resources.Views.UiText.ResourceManager.GetString("ResidentIdCodeAlreadyExists")
                ?? "Resident with this ID code already exists in this company.",
                nameof(dto.IdCode)));
        }

        dto.Id = Guid.Empty;
        dto.ManagementCompanyId = company.Value.ManagementCompanyId;
        dto.FirstName = normalized.FirstName;
        dto.LastName = normalized.LastName;
        dto.IdCode = normalized.IdCode;
        dto.PreferredLanguage = normalized.PreferredLanguage;

        return await AddAndFindCoreAsync(dto, company.Value.ManagementCompanyId, cancellationToken);
    }

    public async Task<Result<ResidentProfileModel>> CreateAndGetProfileAsync(
        ManagementCompanyRoute route,
        ResidentBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var created = await CreateAsync(route, dto, cancellationToken);
        if (created.IsFailed)
        {
            return Result.Fail<ResidentProfileModel>(created.Errors);
        }

        return await GetProfileAsync(
            new ResidentRoute
            {
                AppUserId = route.AppUserId,
                CompanySlug = route.CompanySlug,
                ResidentIdCode = created.Value.IdCode
            },
            cancellationToken);
    }

    public async Task<Result<ResidentBllDto>> UpdateAsync(
        ResidentRoute route,
        ResidentBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveWorkspaceAsync(route, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var validation = Validate(dto);
        if (validation.IsFailed)
        {
            return Result.Fail<ResidentBllDto>(validation.Errors);
        }

        var normalized = Normalize(dto);
        var duplicateIdCode = await ServiceUOW.Residents.IdCodeExistsForCompanyAsync(
            workspace.Value.ManagementCompanyId,
            normalized.IdCode,
            workspace.Value.ResidentId,
            cancellationToken);
        if (duplicateIdCode)
        {
            return Result.Fail(new DuplicateResidentIdCodeError(
                App.Resources.Views.UiText.ResourceManager.GetString("ResidentIdCodeAlreadyExists")
                ?? "Resident with this ID code already exists in this company.",
                nameof(dto.IdCode)));
        }

        dto.Id = workspace.Value.ResidentId;
        dto.ManagementCompanyId = workspace.Value.ManagementCompanyId;
        dto.FirstName = normalized.FirstName;
        dto.LastName = normalized.LastName;
        dto.IdCode = normalized.IdCode;
        dto.PreferredLanguage = normalized.PreferredLanguage;

        var updated = await base.UpdateAsync(dto, workspace.Value.ManagementCompanyId, cancellationToken);
        if (updated.IsFailed)
        {
            return Result.Fail<ResidentBllDto>(updated.Errors);
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return updated;
    }

    public async Task<Result<ResidentProfileModel>> UpdateAndGetProfileAsync(
        ResidentRoute route,
        ResidentBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var updated = await UpdateAsync(route, dto, cancellationToken);
        if (updated.IsFailed)
        {
            return Result.Fail<ResidentProfileModel>(updated.Errors);
        }

        return await GetProfileAsync(
            new ResidentRoute
            {
                AppUserId = route.AppUserId,
                CompanySlug = route.CompanySlug,
                ResidentIdCode = updated.Value.IdCode
            },
            cancellationToken);
    }

    public async Task<Result> DeleteAsync(
        ResidentRoute route,
        string confirmationIdCode,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveWorkspaceAsync(route, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var profile = await ServiceUOW.Residents.FindProfileAsync(
            workspace.Value.ResidentId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (profile is null)
        {
            return Result.Fail(new NotFoundError("Resident profile was not found."));
        }

        if (!string.Equals(confirmationIdCode?.Trim(), profile.IdCode.Trim(), StringComparison.Ordinal))
        {
            return Result.Fail(new ValidationAppError(
                "Delete confirmation does not match the current resident ID code.",
                [
                    new ValidationFailureModel
                    {
                        PropertyName = "ConfirmationIdCode",
                        ErrorMessage = "Delete confirmation does not match the current resident ID code."
                    }
                ]));
        }

        var roleCode = await ServiceUOW.ManagementCompanies.FindActiveUserRoleCodeAsync(
            route.AppUserId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (roleCode is null || !DeleteAllowedRoleCodes.Contains(roleCode))
        {
            return Result.Fail(new ForbiddenError("Access denied."));
        }

        var canDelete = await _deleteGuard.CanDeleteResidentAsync(
            workspace.Value.ResidentId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (!canDelete)
        {
            return Result.Fail(new BusinessRuleError(DeleteBlockedMessage()));
        }

        var removed = await base.RemoveAsync(workspace.Value.ResidentId, workspace.Value.ManagementCompanyId, cancellationToken);
        if (removed.IsFailed)
        {
            return Result.Fail(removed.Errors);
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    private async Task<Result<ResidentProfileModel>> GetProfileAsync(
        ResidentWorkspaceModel workspace,
        CancellationToken cancellationToken)
    {
        var profile = await ServiceUOW.Residents.FindProfileAsync(
            workspace.ResidentId,
            workspace.ManagementCompanyId,
            cancellationToken);

        return profile is null
            ? Result.Fail(new NotFoundError("Resident profile was not found."))
            : Result.Ok(new ResidentProfileModel
            {
                ResidentId = profile.Id,
                ManagementCompanyId = profile.ManagementCompanyId,
                CompanySlug = profile.CompanySlug,
                CompanyName = profile.CompanyName,
                ResidentIdCode = profile.IdCode,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                FullName = BuildFullName(profile.FirstName, profile.LastName),
                PreferredLanguage = profile.PreferredLanguage
            });
    }

    private static Result Validate(ResidentBllDto dto)
    {
        var failures = new List<ValidationFailureModel>();

        if (string.IsNullOrWhiteSpace(dto.FirstName))
        {
            failures.Add(CreateRequiredFailure(nameof(dto.FirstName), App.Resources.Views.UiText.FirstName));
        }

        if (string.IsNullOrWhiteSpace(dto.LastName))
        {
            failures.Add(CreateRequiredFailure(nameof(dto.LastName), App.Resources.Views.UiText.LastName));
        }

        if (string.IsNullOrWhiteSpace(dto.IdCode))
        {
            failures.Add(CreateRequiredFailure(nameof(dto.IdCode), App.Resources.Views.UiText.IdCode));
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

    private static NormalizedResident Normalize(ResidentBllDto dto)
    {
        return new NormalizedResident(
            dto.FirstName.Trim(),
            dto.LastName.Trim(),
            dto.IdCode.Trim(),
            string.IsNullOrWhiteSpace(dto.PreferredLanguage)
                ? null
                : dto.PreferredLanguage.Trim());
    }

    private static string BuildFullName(string firstName, string lastName)
    {
        return string.Join(
            " ",
            new[] { firstName, lastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private sealed record NormalizedResident(
        string FirstName,
        string LastName,
        string IdCode,
        string? PreferredLanguage);

    private static string DeleteBlockedMessage()
    {
        return App.Resources.Views.UiText.ResourceManager.GetString("UnableToDeleteBecauseDependentRecordsExist")
               ?? "Unable to delete because dependent records exist.";
    }
}
