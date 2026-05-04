using App.BLL.Contracts.Common;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Units.Commands;
using App.BLL.Contracts.Units.Models;
using App.BLL.Contracts.Units.Queries;
using App.BLL.Contracts.Units.Services;
using App.BLL.Mappers.Units;
using App.DAL.Contracts;
using App.DAL.Contracts.DAL.Units;
using FluentResults;

namespace App.BLL.Units;

public class UnitProfileService : IUnitProfileService
{
    private static readonly HashSet<string> DeleteAllowedRoleCodes =
    [
        "OWNER",
        "MANAGER"
    ];

    private readonly IUnitAccessService _unitAccessService;
    private readonly IAppUOW _uow;

    public UnitProfileService(
        IUnitAccessService unitAccessService,
        IAppUOW uow)
    {
        _unitAccessService = unitAccessService;
        _uow = uow;
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
            new UnitUpdateDalDto
            {
                Id = workspace.Value.UnitId,
                PropertyId = workspace.Value.PropertyId,
                UnitNr = command.UnitNr.Trim(),
                FloorNr = command.FloorNr,
                SizeM2 = command.SizeM2,
                Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim(),
                IsActive = command.IsActive
            },
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

        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var deleted = await _uow.Units.DeleteAsync(
                workspace.Value.UnitId,
                workspace.Value.PropertyId,
                workspace.Value.ManagementCompanyId,
                cancellationToken);

            if (!deleted)
            {
                await _uow.RollbackTransactionAsync(cancellationToken);
                return Result.Fail(new NotFoundError("Unit profile was not found."));
            }

            await _uow.SaveChangesAsync(cancellationToken);
            await _uow.CommitTransactionAsync(cancellationToken);
            return Result.Ok();
        }
        catch
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
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
}
