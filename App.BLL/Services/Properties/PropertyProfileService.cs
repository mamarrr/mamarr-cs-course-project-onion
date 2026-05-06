using App.BLL.Contracts.Common;
using App.BLL.Contracts.Common.Deletion;
using App.BLL.Contracts.Properties;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Properties.Commands;
using App.BLL.DTO.Properties.Models;
using App.BLL.DTO.Properties.Queries;
using App.BLL.Mappers.Properties;
using App.DAL.Contracts;
using App.DAL.DTO.Properties;
using FluentResults;

namespace App.BLL.Services.Properties;

public class PropertyProfileService : IPropertyProfileService
{
    private static readonly HashSet<string> DeleteAllowedRoleCodes =
    [
        "OWNER",
        "MANAGER"
    ];

    private readonly IPropertyWorkspaceService _propertyWorkspaceService;
    private readonly IAppUOW _uow;
    private readonly IAppDeleteGuard _deleteGuard;

    public PropertyProfileService(
        IPropertyWorkspaceService propertyWorkspaceService,
        IAppUOW uow,
        IAppDeleteGuard deleteGuard)
    {
        _propertyWorkspaceService = propertyWorkspaceService;
        _uow = uow;
        _deleteGuard = deleteGuard;
    }

    public async Task<Result<PropertyProfileModel>> GetAsync(
        GetPropertyProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveWorkspaceAsync(
            query.UserId,
            query.CompanySlug,
            query.CustomerSlug,
            query.PropertySlug,
            cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        return await GetProfileAsync(workspace.Value, cancellationToken);
    }

    public async Task<Result<PropertyProfileModel>> UpdateAsync(
        UpdatePropertyProfileCommand command,
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
            cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var profile = await _uow.Properties.FindProfileAsync(
            workspace.Value.PropertyId,
            workspace.Value.CustomerId,
            cancellationToken);

        if (profile is null)
        {
            return Result.Fail(new NotFoundError("Property profile was not found."));
        }

        var normalized = NormalizeUpdate(command);
        await _uow.Properties.UpdateAsync(
            new PropertyDalDto
            {
                Id = workspace.Value.PropertyId,
                CustomerId = workspace.Value.CustomerId,
                PropertyTypeId = profile.PropertyTypeId,
                Label = normalized.Name,
                Slug = profile.Slug,
                AddressLine = normalized.AddressLine,
                City = normalized.City,
                PostalCode = normalized.PostalCode,
                Notes = normalized.Notes,
            },
            workspace.Value.CustomerId,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return await GetProfileAsync(workspace.Value, cancellationToken);
    }

    public async Task<Result> DeleteAsync(
        DeletePropertyCommand command,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveWorkspaceAsync(
            command.UserId,
            command.CompanySlug,
            command.CustomerSlug,
            command.PropertySlug,
            cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var profile = await _uow.Properties.FindProfileAsync(
            workspace.Value.PropertyId,
            workspace.Value.CustomerId,
            cancellationToken);

        if (profile is null)
        {
            return Result.Fail(new NotFoundError("Property profile was not found."));
        }

        if (!string.Equals(command.ConfirmationName?.Trim(), profile.Name.Trim(), StringComparison.Ordinal))
        {
            return Result.Fail(new ValidationAppError(
                "Delete confirmation does not match the current property name.",
                [
                    new ValidationFailureModel
                    {
                        PropertyName = nameof(command.ConfirmationName),
                        ErrorMessage = "Delete confirmation does not match the current property name."
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

        var canDelete = await _deleteGuard.CanDeletePropertyAsync(
            workspace.Value.PropertyId,
            workspace.Value.CustomerId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (!canDelete)
        {
            return Result.Fail(new BusinessRuleError(DeleteBlockedMessage()));
        }

        await _uow.Properties.RemoveAsync(
            workspace.Value.PropertyId,
            workspace.Value.CustomerId,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    private async Task<Result<PropertyWorkspaceModel>> ResolveWorkspaceAsync(
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

    private async Task<Result<PropertyProfileModel>> GetProfileAsync(
        PropertyWorkspaceModel workspace,
        CancellationToken cancellationToken)
    {
        var profile = await _uow.Properties.FindProfileAsync(
            workspace.PropertyId,
            workspace.CustomerId,
            cancellationToken);

        return profile is null
            ? Result.Fail(new NotFoundError("Property profile was not found."))
            : Result.Ok(PropertyBllMapper.MapProfile(profile));
    }

    private static Result ValidateUpdate(UpdatePropertyProfileCommand command)
    {
        var failures = new List<ValidationFailureModel>();

        AddRequired(failures, nameof(command.Name), command.Name, App.Resources.Views.UiText.Name);
        AddRequired(failures, nameof(command.AddressLine), command.AddressLine, App.Resources.Views.UiText.AddressLine);
        AddRequired(failures, nameof(command.City), command.City, App.Resources.Views.UiText.City);
        AddRequired(failures, nameof(command.PostalCode), command.PostalCode, App.Resources.Views.UiText.PostalCode);

        return failures.Count == 0
            ? Result.Ok()
            : Result.Fail(new ValidationAppError("Validation failed.", failures));
    }

    private static void AddRequired(
        ICollection<ValidationFailureModel> failures,
        string propertyName,
        string? value,
        string label)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        failures.Add(new ValidationFailureModel
        {
            PropertyName = propertyName,
            ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", label)
        });
    }

    private static NormalizedUpdate NormalizeUpdate(UpdatePropertyProfileCommand command)
    {
        return new NormalizedUpdate(
            command.Name.Trim(),
            command.AddressLine.Trim(),
            command.City.Trim(),
            command.PostalCode.Trim(),
            string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim());
    }

    private sealed record NormalizedUpdate(
        string Name,
        string AddressLine,
        string City,
        string PostalCode,
        string? Notes);

    private static string DeleteBlockedMessage()
    {
        return App.Resources.Views.UiText.ResourceManager.GetString("UnableToDeleteBecauseDependentRecordsExist")
               ?? "Unable to delete because dependent records exist.";
    }
}
