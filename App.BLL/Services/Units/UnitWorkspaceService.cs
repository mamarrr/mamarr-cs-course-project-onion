using App.BLL.Contracts.Common;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Properties;
using App.BLL.Contracts.Properties.Queries;
using App.BLL.Contracts.Units;
using App.BLL.Contracts.Units.Commands;
using App.BLL.Contracts.Units.Models;
using App.BLL.Contracts.Units.Queries;
using App.BLL.Mappers.Units;
using App.BLL.Shared.Routing;
using App.DAL.Contracts;
using App.DAL.DTO.Units;
using FluentResults;

namespace App.BLL.Services.Units;

public class UnitWorkspaceService : IUnitWorkspaceService
{
    private const int MinFloorNr = -200;
    private const int MaxFloorNr = 300;
    private const decimal MinSizeM2 = 0m;
    private const decimal MaxSizeM2 = 99999999.99m;

    private readonly IPropertyWorkspaceService _propertyWorkspaceService;
    private readonly IUnitAccessService _unitAccessService;
    private readonly IAppUOW _uow;

    public UnitWorkspaceService(
        IPropertyWorkspaceService propertyWorkspaceService,
        IUnitAccessService unitAccessService,
        IAppUOW uow)
    {
        _propertyWorkspaceService = propertyWorkspaceService;
        _unitAccessService = unitAccessService;
        _uow = uow;
    }

    public async Task<Result<UnitDashboardModel>> GetDashboardAsync(
        GetUnitDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _unitAccessService.ResolveUnitWorkspaceAsync(query, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        return Result.Ok(new UnitDashboardModel
        {
            Workspace = workspace.Value
        });
    }

    public async Task<Result<PropertyUnitsModel>> GetPropertyUnitsAsync(
        GetPropertyUnitsQuery query,
        CancellationToken cancellationToken = default)
    {
        var property = await ResolvePropertyAsync(
            query.UserId,
            query.CompanySlug,
            query.CustomerSlug,
            query.PropertySlug,
            cancellationToken);
        if (property.IsFailed)
        {
            return Result.Fail(property.Errors);
        }

        var units = await _uow.Units.AllByPropertyAsync(
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
            Units = units.Select(UnitBllMapper.MapListItem).ToList()
        });
    }

    public async Task<Result<UnitProfileModel>> CreateAsync(
        CreateUnitCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateCreate(command);
        if (validation.IsFailed)
        {
            return Result.Fail(validation.Errors);
        }

        var property = await ResolvePropertyAsync(
            command.UserId,
            command.CompanySlug,
            command.CustomerSlug,
            command.PropertySlug,
            cancellationToken);
        if (property.IsFailed)
        {
            return Result.Fail(property.Errors);
        }

        var normalizedUnitNr = command.UnitNr.Trim();
        var baseSlug = SlugGenerator.GenerateSlug(normalizedUnitNr);
        var existingSlugs = await _uow.Units.AllSlugsByPropertyWithPrefixAsync(
            property.Value.PropertyId,
            baseSlug,
            cancellationToken);
        var uniqueSlug = SlugGenerator.EnsureUniqueSlug(baseSlug, existingSlugs);

        var unitId = Guid.NewGuid();
        _uow.Units.Add(new UnitDalDto
        {
            Id = unitId,
            PropertyId = property.Value.PropertyId,
            UnitNr = normalizedUnitNr,
            Slug = uniqueSlug,
            FloorNr = command.FloorNr,
            SizeM2 = command.SizeM2,
            Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim()
        });

        await _uow.SaveChangesAsync(cancellationToken);

        var profile = await _uow.Units.FindProfileAsync(
            unitId,
            property.Value.PropertyId,
            cancellationToken);

        return profile is null
            ? Result.Fail(new NotFoundError("Unit profile was not found."))
            : Result.Ok(UnitBllMapper.MapProfile(profile));
    }

    private async Task<Result<App.BLL.Contracts.Properties.Models.PropertyWorkspaceModel>> ResolvePropertyAsync(
        Guid userId,
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        return await _propertyWorkspaceService.GetWorkspaceAsync(
            new GetPropertyWorkspaceQuery
            {
                UserId = userId,
                CompanySlug = companySlug,
                CustomerSlug = customerSlug,
                PropertySlug = propertySlug
            },
            cancellationToken);
    }

    private static Result ValidateCreate(CreateUnitCommand command)
    {
        var failures = new List<ValidationFailureModel>();

        if (string.IsNullOrWhiteSpace(command.UnitNr))
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(command.UnitNr),
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace(
                    "{0}",
                    App.Resources.Views.UiText.ResourceManager.GetString("UnitNr") ?? "Unit number")
            });
        }

        if (command.FloorNr.HasValue &&
            (command.FloorNr.Value < MinFloorNr || command.FloorNr.Value > MaxFloorNr))
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(command.FloorNr),
                ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("InvalidData")
                               ?? $"Floor number must be between {MinFloorNr} and {MaxFloorNr}."
            });
        }

        if (command.SizeM2.HasValue &&
            (command.SizeM2.Value < MinSizeM2 || command.SizeM2.Value > MaxSizeM2))
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(command.SizeM2),
                ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("InvalidData")
                               ?? $"Size must be between {MinSizeM2} and {MaxSizeM2}."
            });
        }

        return failures.Count == 0
            ? Result.Ok()
            : Result.Fail(new ValidationAppError("Validation failed.", failures));
    }
}
