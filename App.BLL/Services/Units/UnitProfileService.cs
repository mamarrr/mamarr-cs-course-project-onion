using App.BLL.Contracts.Common;
using App.BLL.Contracts.Common.Deletion;
using App.BLL.Contracts.Units;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Units.Commands;
using App.BLL.DTO.Units.Models;
using App.BLL.DTO.Units.Queries;
using App.BLL.Mappers.Units;
using App.DAL.Contracts;
using App.DAL.DTO.Units;
using FluentResults;

namespace App.BLL.Services.Units;

public class UnitProfileService : IUnitProfileService
{
    private static readonly HashSet<string> DeleteAllowedRoleCodes =
    [
        "OWNER",
        "MANAGER"
    ];

    private readonly IUnitAccessService _unitAccessService;
    private readonly IAppUOW _uow;
    private readonly IAppDeleteGuard _deleteGuard;

    public UnitProfileService(
        IUnitAccessService unitAccessService,
        IAppUOW uow,
        IAppDeleteGuard deleteGuard)
    {
        _unitAccessService = unitAccessService;
        _uow = uow;
        _deleteGuard = deleteGuard;
    }

    public async Task<Result<UnitProfileModel>> GetAsync(
        GetUnitProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveWorkspaceAsync(
            query.UserId,
            query.CompanySlug,
            query.CustomerSlug,
            query.PropertySlug,
            query.UnitSlug,
            cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        return await GetProfileAsync(workspace.Value, cancellationToken);
    }

    public async Task<Result<UnitProfileModel>> UpdateAsync(
        UpdateUnitCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateUpdate(command);
        if (validation.IsFailed)
        {
            return Result.Fail(validation.Errors);
        }

        var workspace = await ResolveWorkspaceAsync(
            command.UserId,
            command.CompanySlug,
            command.CustomerSlug,
            command.PropertySlug,
            command.UnitSlug,
            cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var profile = await _uow.Units.FindProfileAsync(
            workspace.Value.UnitId,
            workspace.Value.PropertyId,
            cancellationToken);
        if (profile is null)
        {
            return Result.Fail(new NotFoundError("Unit profile was not found."));
        }

        await _uow.Units.UpdateAsync(
            new UnitDalDto
            {
                Id = workspace.Value.UnitId,
                PropertyId = workspace.Value.PropertyId,
                UnitNr = command.UnitNr.Trim(),
                Slug = profile.Slug,
                FloorNr = command.FloorNr,
                SizeM2 = command.SizeM2,
                Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim(),
            },
            workspace.Value.PropertyId,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return await GetProfileAsync(workspace.Value, cancellationToken);
    }

    public async Task<Result> DeleteAsync(
        DeleteUnitCommand command,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveWorkspaceAsync(
            command.UserId,
            command.CompanySlug,
            command.CustomerSlug,
            command.PropertySlug,
            command.UnitSlug,
            cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var profile = await _uow.Units.FindProfileAsync(
            workspace.Value.UnitId,
            workspace.Value.PropertyId,
            cancellationToken);
        if (profile is null)
        {
            return Result.Fail(new NotFoundError("Unit profile was not found."));
        }

        if (!string.Equals(command.ConfirmationUnitNr?.Trim(), profile.UnitNr.Trim(), StringComparison.Ordinal))
        {
            return Result.Fail(new ValidationAppError(
                "Delete confirmation does not match the current unit number.",
                [
                    new ValidationFailureModel
                    {
                        PropertyName = nameof(command.ConfirmationUnitNr),
                        ErrorMessage = "Delete confirmation does not match the current unit number."
                    }
                ]));
        }

        var roleCode = await _uow.Customers.FindActiveManagementCompanyRoleCodeAsync(
            workspace.Value.ManagementCompanyId,
            command.UserId,
            cancellationToken);
        if (roleCode is null || !DeleteAllowedRoleCodes.Contains(roleCode.ToUpperInvariant()))
        {
            return Result.Fail(new ForbiddenError(App.Resources.Views.UiText.AccessDeniedDescription));
        }

        var canDelete = await _deleteGuard.CanDeleteUnitAsync(
            workspace.Value.UnitId,
            workspace.Value.PropertyId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (!canDelete)
        {
            return Result.Fail(new BusinessRuleError(DeleteBlockedMessage()));
        }

        await _uow.Units.RemoveAsync(
            workspace.Value.UnitId,
            workspace.Value.PropertyId,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    private async Task<Result<UnitWorkspaceModel>> ResolveWorkspaceAsync(
        Guid userId,
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        return await _unitAccessService.ResolveUnitWorkspaceAsync(
            new GetUnitDashboardQuery
            {
                UserId = userId,
                CompanySlug = companySlug,
                CustomerSlug = customerSlug,
                PropertySlug = propertySlug,
                UnitSlug = unitSlug
            },
            cancellationToken);
    }

    private async Task<Result<UnitProfileModel>> GetProfileAsync(
        UnitWorkspaceModel workspace,
        CancellationToken cancellationToken)
    {
        var profile = await _uow.Units.FindProfileAsync(
            workspace.UnitId,
            workspace.PropertyId,
            cancellationToken);

        return profile is null
            ? Result.Fail(new NotFoundError("Unit profile was not found."))
            : Result.Ok(UnitBllMapper.MapProfile(profile));
    }

    private static Result ValidateUpdate(UpdateUnitCommand command)
    {
        if (!string.IsNullOrWhiteSpace(command.UnitNr))
        {
            return Result.Ok();
        }

        return Result.Fail(new ValidationAppError(
            "Validation failed.",
            [
                new ValidationFailureModel
                {
                    PropertyName = nameof(command.UnitNr),
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
