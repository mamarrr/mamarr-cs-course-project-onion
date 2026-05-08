using App.BLL.Contracts.Common.Portal;
using App.BLL.Contracts.Units;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Properties.Models;
using App.BLL.DTO.Units;
using App.BLL.DTO.Units.Models;
using App.BLL.Shared.Routing;
using App.BLL.Mappers.Units;
using App.DAL.Contracts;
using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Units;
using Base.BLL;
using FluentResults;

namespace App.BLL.Services.Units;

public class UnitService :
    BaseService<UnitBllDto, UnitDalDto, IUnitRepository, IAppUOW>,
    IUnitService
{
    private const int MinFloorNr = -200;
    private const int MaxFloorNr = 300;
    private const decimal MinSizeM2 = 0m;
    private const decimal MaxSizeM2 = 99999999.99m;

    private static readonly HashSet<string> DeleteAllowedRoleCodes =
    [
        "OWNER",
        "MANAGER"
    ];

    private static readonly HashSet<string> AccessAllowedRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER",
        "FINANCE",
        "SUPPORT"
    };

    private readonly IPortalContextProvider _portalContext;

    public UnitService(
        IAppUOW uow,
        IPortalContextProvider portalContext)
        : base(uow.Units, uow, new UnitBllDtoMapper())
    {
        _portalContext = portalContext;
    }

    public async Task<Result<UnitWorkspaceModel>> ResolveWorkspaceAsync(
        UnitRoute route,
        CancellationToken cancellationToken = default)
    {
        return await _portalContext.ResolveUnitWorkspaceAsync(route, cancellationToken);
    }

    public async Task<Result<PropertyUnitsModel>> ListForPropertyAsync(
        PropertyRoute route,
        CancellationToken cancellationToken = default)
    {
        var property = await ResolvePropertyWorkspaceAsync(route.AppUserId, route.CompanySlug, route.CustomerSlug, route.PropertySlug, cancellationToken);
        if (property.IsFailed)
        {
            return Result.Fail(property.Errors);
        }

        var units = await ServiceUOW.Units.AllByPropertyAsync(
            property.Value.PropertyId,
            cancellationToken);

        return Result.Ok(new PropertyUnitsModel
        {
            AppUserId = property.Value.AppUserId,
            ManagementCompanyId = property.Value.ManagementCompanyId,
            CompanySlug = property.Value.CompanySlug,
            CompanyName = property.Value.CompanyName,
            CustomerId = property.Value.CustomerId,
            CustomerSlug = property.Value.CustomerSlug,
            CustomerName = property.Value.CustomerName,
            PropertyId = property.Value.PropertyId,
            PropertySlug = property.Value.PropertySlug,
            PropertyName = property.Value.PropertyName,
            Units = units.Select(unit => new UnitListItemModel
            {
                UnitId = unit.Id,
                PropertyId = unit.PropertyId,
                UnitSlug = unit.Slug,
                UnitNr = unit.UnitNr,
                FloorNr = unit.FloorNr,
                SizeM2 = unit.SizeM2
            }).ToList()
        });
    }

    public async Task<Result<UnitDashboardModel>> GetDashboardAsync(
        UnitRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveWorkspaceAsync(route, cancellationToken);
        return workspace.IsFailed
            ? Result.Fail<UnitDashboardModel>(workspace.Errors)
            : Result.Ok(new UnitDashboardModel { Workspace = workspace.Value });
    }

    public async Task<Result<UnitProfileModel>> GetProfileAsync(
        UnitRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveWorkspaceAsync(route, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        return await GetProfileAsync(workspace.Value, cancellationToken);
    }

    public async Task<Result<UnitBllDto>> CreateAsync(
        PropertyRoute route,
        UnitBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var property = await ResolvePropertyWorkspaceAsync(route.AppUserId, route.CompanySlug, route.CustomerSlug, route.PropertySlug, cancellationToken);
        if (property.IsFailed)
        {
            return Result.Fail(property.Errors);
        }

        var validation = ValidateCreate(dto);
        if (validation.IsFailed)
        {
            return Result.Fail<UnitBllDto>(validation.Errors);
        }

        var normalizedUnitNr = dto.UnitNr.Trim();
        var baseSlug = SlugGenerator.GenerateSlug(normalizedUnitNr);
        var existingSlugs = await ServiceUOW.Units.AllSlugsByPropertyWithPrefixAsync(
            property.Value.PropertyId,
            baseSlug,
            cancellationToken);

        dto.Id = Guid.Empty;
        dto.PropertyId = property.Value.PropertyId;
        dto.UnitNr = normalizedUnitNr;
        dto.Slug = SlugGenerator.EnsureUniqueSlug(baseSlug, existingSlugs);
        dto.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();

        return await AddAndFindCoreAsync(dto, property.Value.PropertyId, cancellationToken);
    }

    public async Task<Result<UnitProfileModel>> CreateAndGetProfileAsync(
        PropertyRoute route,
        UnitBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var created = await CreateAsync(route, dto, cancellationToken);
        if (created.IsFailed)
        {
            return Result.Fail<UnitProfileModel>(created.Errors);
        }

        return await GetProfileAsync(
            new UnitRoute
            {
                AppUserId = route.AppUserId,
                CompanySlug = route.CompanySlug,
                CustomerSlug = route.CustomerSlug,
                PropertySlug = route.PropertySlug,
                UnitSlug = created.Value.Slug
            },
            cancellationToken);
    }

    public async Task<Result<UnitBllDto>> UpdateAsync(
        UnitRoute route,
        UnitBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveWorkspaceAsync(route, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var profile = await ServiceUOW.Units.FindProfileAsync(
            workspace.Value.UnitId,
            workspace.Value.PropertyId,
            cancellationToken);
        if (profile is null)
        {
            return Result.Fail(new NotFoundError("Unit profile was not found."));
        }

        var validation = ValidateUpdate(dto);
        if (validation.IsFailed)
        {
            return Result.Fail<UnitBllDto>(validation.Errors);
        }

        dto.Id = workspace.Value.UnitId;
        dto.PropertyId = workspace.Value.PropertyId;
        dto.UnitNr = dto.UnitNr.Trim();
        dto.Slug = profile.Slug;
        dto.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();

        var updated = await base.UpdateAsync(dto, workspace.Value.PropertyId, cancellationToken);
        if (updated.IsFailed)
        {
            return Result.Fail<UnitBllDto>(updated.Errors);
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return updated;
    }

    public async Task<Result<UnitProfileModel>> UpdateAndGetProfileAsync(
        UnitRoute route,
        UnitBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var updated = await UpdateAsync(route, dto, cancellationToken);
        return updated.IsFailed
            ? Result.Fail<UnitProfileModel>(updated.Errors)
            : await GetProfileAsync(route, cancellationToken);
    }

    public async Task<Result> DeleteAsync(
        UnitRoute route,
        string confirmationUnitNr,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveWorkspaceAsync(route, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var profile = await ServiceUOW.Units.FindProfileAsync(
            workspace.Value.UnitId,
            workspace.Value.PropertyId,
            cancellationToken);
        if (profile is null)
        {
            return Result.Fail(new NotFoundError("Unit profile was not found."));
        }

        if (!string.Equals(confirmationUnitNr?.Trim(), profile.UnitNr.Trim(), StringComparison.Ordinal))
        {
            return Result.Fail(new ValidationAppError(
                "Delete confirmation does not match the current unit number.",
                [
                    new ValidationFailureModel
                    {
                        PropertyName = "ConfirmationUnitNr",
                        ErrorMessage = "Delete confirmation does not match the current unit number."
                    }
                ]));
        }

        var roleCode = await ServiceUOW.Customers.FindActiveManagementCompanyRoleCodeAsync(
            workspace.Value.ManagementCompanyId,
            route.AppUserId,
            cancellationToken);
        if (roleCode is null || !DeleteAllowedRoleCodes.Contains(roleCode.ToUpperInvariant()))
        {
            return Result.Fail(new ForbiddenError(App.Resources.Views.UiText.AccessDeniedDescription));
        }

        var hasDependencies = await ServiceUOW.Units.HasDeleteDependenciesAsync(
            workspace.Value.UnitId,
            workspace.Value.PropertyId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (hasDependencies)
        {
            return Result.Fail(new BusinessRuleError(DeleteBlockedMessage()));
        }

        var removed = await base.RemoveAsync(workspace.Value.UnitId, workspace.Value.PropertyId, cancellationToken);
        if (removed.IsFailed)
        {
            return Result.Fail(removed.Errors);
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    private async Task<Result<PropertyWorkspaceModel>> ResolvePropertyWorkspaceAsync(
        Guid userId,
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        return await _portalContext.ResolvePropertyWorkspaceAsync(
            new PropertyRoute
            {
                AppUserId = userId,
                CompanySlug = companySlug,
                CustomerSlug = customerSlug,
                PropertySlug = propertySlug
            },
            AccessAllowedRoleCodes,
            allowCustomerContext: true,
            cancellationToken);
    }

    private async Task<Result<UnitProfileModel>> GetProfileAsync(
        UnitWorkspaceModel workspace,
        CancellationToken cancellationToken)
    {
        var profile = await ServiceUOW.Units.FindProfileAsync(
            workspace.UnitId,
            workspace.PropertyId,
            cancellationToken);

        return profile is null
            ? Result.Fail(new NotFoundError("Unit profile was not found."))
            : Result.Ok(new UnitProfileModel
            {
                UnitId = profile.Id,
                PropertyId = profile.PropertyId,
                CustomerId = profile.CustomerId,
                ManagementCompanyId = profile.ManagementCompanyId,
                CompanySlug = profile.CompanySlug,
                CompanyName = profile.CompanyName,
                CustomerSlug = profile.CustomerSlug,
                CustomerName = profile.CustomerName,
                PropertySlug = profile.PropertySlug,
                PropertyName = profile.PropertyName,
                UnitSlug = profile.Slug,
                UnitNr = profile.UnitNr,
                FloorNr = profile.FloorNr,
                SizeM2 = profile.SizeM2,
                Notes = profile.Notes
            });
    }

    private static Result ValidateCreate(UnitBllDto dto)
    {
        var failures = new List<ValidationFailureModel>();

        if (string.IsNullOrWhiteSpace(dto.UnitNr))
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(dto.UnitNr),
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace(
                    "{0}",
                    App.Resources.Views.UiText.ResourceManager.GetString("UnitNr") ?? "Unit number")
            });
        }

        if (dto.FloorNr.HasValue &&
            (dto.FloorNr.Value < MinFloorNr || dto.FloorNr.Value > MaxFloorNr))
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(dto.FloorNr),
                ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("InvalidData")
                               ?? $"Floor number must be between {MinFloorNr} and {MaxFloorNr}."
            });
        }

        if (dto.SizeM2.HasValue &&
            (dto.SizeM2.Value < MinSizeM2 || dto.SizeM2.Value > MaxSizeM2))
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(dto.SizeM2),
                ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("InvalidData")
                               ?? $"Size must be between {MinSizeM2} and {MaxSizeM2}."
            });
        }

        return failures.Count == 0
            ? Result.Ok()
            : Result.Fail(new ValidationAppError("Validation failed.", failures));
    }

    private static Result ValidateUpdate(UnitBllDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.UnitNr))
        {
            return Result.Ok();
        }

        return Result.Fail(new ValidationAppError(
            "Validation failed.",
            [
                new ValidationFailureModel
                {
                    PropertyName = nameof(dto.UnitNr),
                    ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace(
                        "{0}",
                        App.Resources.Views.UiText.UnitNr)
                }
            ]));
    }

    private static string DeleteBlockedMessage()
    {
        return App.Resources.Views.UiText.ResourceManager.GetString("UnableToDeleteBecauseDependentRecordsExist")
               ?? "Unable to delete because dependent records exist.";
    }
}
