using App.BLL.Contracts.Common;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Residents;
using App.BLL.Contracts.Residents.Commands;
using App.BLL.Contracts.Residents.Errors;
using App.BLL.Contracts.Residents.Models;
using App.BLL.Contracts.Residents.Queries;
using App.BLL.Mappers.Residents;
using App.DAL.Contracts;
using App.DAL.DTO.Residents;
using FluentResults;

namespace App.BLL.Services.Residents;

public class ResidentProfileService : IResidentProfileService
{
    private static readonly HashSet<string> DeleteAllowedRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER"
    };

    private readonly IResidentAccessService _residentAccessService;
    private readonly IAppUOW _uow;

    public ResidentProfileService(
        IResidentAccessService residentAccessService,
        IAppUOW uow)
    {
        _residentAccessService = residentAccessService;
        _uow = uow;
    }

    public async Task<Result<ResidentProfileModel>> GetAsync(
        GetResidentProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _residentAccessService.ResolveResidentWorkspaceAsync(query, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        return await GetProfileAsync(workspace.Value, cancellationToken);
    }

    public async Task<Result<ResidentProfileModel>> UpdateAsync(
        UpdateResidentProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateUpdate(command);
        if (validation.IsFailed)
        {
            return Result.Fail(validation.Errors);
        }

        var workspace = await _residentAccessService.ResolveResidentWorkspaceAsync(
            new GetResidentProfileQuery
            {
                UserId = command.UserId,
                CompanySlug = command.CompanySlug,
                ResidentIdCode = command.ResidentIdCode
            },
            cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var normalizedFirstName = command.FirstName.Trim();
        var normalizedLastName = command.LastName.Trim();
        var normalizedIdCode = command.IdCode.Trim();
        var normalizedPreferredLanguage = string.IsNullOrWhiteSpace(command.PreferredLanguage)
            ? null
            : command.PreferredLanguage.Trim();

        var duplicateIdCode = await _uow.Residents.IdCodeExistsForCompanyAsync(
            workspace.Value.ManagementCompanyId,
            normalizedIdCode,
            workspace.Value.ResidentId,
            cancellationToken);
        if (duplicateIdCode)
        {
            return Result.Fail(new DuplicateResidentIdCodeError(
                App.Resources.Views.UiText.ResourceManager.GetString("ResidentIdCodeAlreadyExists")
                ?? "Resident with this ID code already exists in this company.",
                nameof(command.IdCode)));
        }

        await _uow.Residents.UpdateAsync(
            new ResidentUpdateDalDto
            {
                Id = workspace.Value.ResidentId,
                ManagementCompanyId = workspace.Value.ManagementCompanyId,
                FirstName = normalizedFirstName,
                LastName = normalizedLastName,
                IdCode = normalizedIdCode,
                PreferredLanguage = normalizedPreferredLanguage,
            },
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        var profile = await _uow.Residents.FindProfileAsync(
            workspace.Value.ResidentId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);

        return profile is null
            ? Result.Fail(new NotFoundError("Resident profile was not found."))
            : Result.Ok(ResidentBllMapper.MapProfile(profile));
    }

    public async Task<Result> DeleteAsync(
        DeleteResidentCommand command,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _residentAccessService.ResolveResidentWorkspaceAsync(
            new GetResidentProfileQuery
            {
                UserId = command.UserId,
                CompanySlug = command.CompanySlug,
                ResidentIdCode = command.ResidentIdCode
            },
            cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var profile = await _uow.Residents.FindProfileAsync(
            workspace.Value.ResidentId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (profile is null)
        {
            return Result.Fail(new NotFoundError("Resident profile was not found."));
        }

        if (!string.Equals(command.ConfirmationIdCode?.Trim(), profile.IdCode.Trim(), StringComparison.Ordinal))
        {
            return Result.Fail(new ValidationAppError(
                "Delete confirmation does not match the current resident ID code.",
                [
                    new ValidationFailureModel
                    {
                        PropertyName = nameof(command.ConfirmationIdCode),
                        ErrorMessage = "Delete confirmation does not match the current resident ID code."
                    }
                ]));
        }

        var roleCode = await _uow.ManagementCompanies.FindActiveUserRoleCodeAsync(
            command.UserId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (roleCode is null || !DeleteAllowedRoleCodes.Contains(roleCode))
        {
            return Result.Fail(new ForbiddenError("Access denied."));
        }

        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var deleted = await _uow.Residents.DeleteAsync(
                workspace.Value.ResidentId,
                workspace.Value.ManagementCompanyId,
                cancellationToken);
            if (!deleted)
            {
                await _uow.RollbackTransactionAsync(cancellationToken);
                return Result.Fail(new NotFoundError("Resident profile was not found."));
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

    private async Task<Result<ResidentProfileModel>> GetProfileAsync(
        ResidentWorkspaceModel workspace,
        CancellationToken cancellationToken)
    {
        var profile = await _uow.Residents.FindProfileAsync(
            workspace.ResidentId,
            workspace.ManagementCompanyId,
            cancellationToken);

        return profile is null
            ? Result.Fail(new NotFoundError("Resident profile was not found."))
            : Result.Ok(ResidentBllMapper.MapProfile(profile));
    }

    private static Result ValidateUpdate(UpdateResidentProfileCommand command)
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
