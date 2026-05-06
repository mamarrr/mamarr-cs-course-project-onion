using System.ComponentModel.DataAnnotations;
using App.BLL.Contracts.Common;
using App.BLL.Contracts.Common.Deletion;
using App.BLL.Contracts.Customers;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Customers.Commands;
using App.BLL.DTO.Customers.Errors;
using App.BLL.DTO.Customers.Models;
using App.BLL.DTO.Customers.Queries;
using App.BLL.Mappers.Customers;
using App.DAL.Contracts;
using App.DAL.DTO.Customers;
using FluentResults;

namespace App.BLL.Services.Customers;

public class CustomerProfileService : ICustomerProfileService
{
    private static readonly HashSet<string> DeleteAllowedRoleCodes =
    [
        "OWNER",
        "MANAGER"
    ];

    private readonly ICustomerAccessService _customerAccessService;
    private readonly IAppUOW _uow;
    private readonly IAppDeleteGuard _deleteGuard;

    public CustomerProfileService(
        ICustomerAccessService customerAccessService,
        IAppUOW uow,
        IAppDeleteGuard deleteGuard)
    {
        _customerAccessService = customerAccessService;
        _uow = uow;
        _deleteGuard = deleteGuard;
    }

    public async Task<Result<CustomerProfileModel>> GetAsync(
        GetCustomerProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveAccessAsync(query.UserId, query.CompanySlug, query.CustomerSlug, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail(access.Errors);
        }

        var profile = await _uow.Customers.FindProfileAsync(
            access.Value.CustomerId,
            access.Value.ManagementCompanyId,
            cancellationToken);

        return profile is null
            ? Result.Fail(new NotFoundError("Customer profile was not found."))
            : Result.Ok(CustomerProfileBllMapper.Map(profile));
    }

    public async Task<Result<CustomerProfileModel>> UpdateAsync(
        UpdateCustomerProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateUpdate(command);
        if (validation.IsFailed)
        {
            return Result.Fail(validation.Errors);
        }

        var access = await ResolveAccessAsync(command.UserId, command.CompanySlug, command.CustomerSlug, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail(access.Errors);
        }

        var profile = await _uow.Customers.FindProfileAsync(
            access.Value.CustomerId,
            access.Value.ManagementCompanyId,
            cancellationToken);

        if (profile is null)
        {
            return Result.Fail(new NotFoundError("Customer profile was not found."));
        }

        var normalized = NormalizeUpdate(command);
        var duplicateRegistryCode = await _uow.Customers.RegistryCodeExistsInCompanyAsync(
            access.Value.ManagementCompanyId,
            normalized.RegistryCode,
            access.Value.CustomerId,
            cancellationToken);

        if (duplicateRegistryCode)
        {
            return Result.Fail(new DuplicateRegistryCodeError(
                App.Resources.Views.UiText.ResourceManager.GetString("CustomerRegistryCodeAlreadyExists")
                ?? "Customer with this registry code already exists in this company.",
                nameof(command.RegistryCode)));
        }

        await _uow.Customers.UpdateAsync(
            new CustomerDalDto
            {
                Id = access.Value.CustomerId,
                ManagementCompanyId = access.Value.ManagementCompanyId,
                Name = normalized.Name,
                Slug = profile.Slug,
                RegistryCode = normalized.RegistryCode,
                BillingEmail = normalized.BillingEmail,
                BillingAddress = normalized.BillingAddress,
                Phone = normalized.Phone,
            },
            access.Value.ManagementCompanyId,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        var updatedProfile = await _uow.Customers.FindProfileAsync(
            access.Value.CustomerId,
            access.Value.ManagementCompanyId,
            cancellationToken);

        return updatedProfile is null
            ? Result.Fail(new NotFoundError("Customer profile was not found."))
            : Result.Ok(CustomerProfileBllMapper.Map(updatedProfile));
    }

    public async Task<Result> DeleteAsync(
        DeleteCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveAccessAsync(command.UserId, command.CompanySlug, command.CustomerSlug, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail(access.Errors);
        }

        var profile = await _uow.Customers.FindProfileAsync(
            access.Value.CustomerId,
            access.Value.ManagementCompanyId,
            cancellationToken);

        if (profile is null)
        {
            return Result.Fail(new NotFoundError("Customer profile was not found."));
        }

        if (!string.Equals(command.ConfirmationName?.Trim(), profile.Name.Trim(), StringComparison.Ordinal))
        {
            return Result.Fail(new ValidationAppError(
                "Delete confirmation does not match the current customer name.",
                [
                    new ValidationFailureModel
                    {
                        PropertyName = nameof(command.ConfirmationName),
                        ErrorMessage = "Delete confirmation does not match the current customer name."
                    }
                ]));
        }

        var roleCode = await _uow.Customers.FindActiveManagementCompanyRoleCodeAsync(
            access.Value.ManagementCompanyId,
            command.UserId,
            cancellationToken);

        if (roleCode is null || !DeleteAllowedRoleCodes.Contains(roleCode.ToUpperInvariant()))
        {
            return Result.Fail(new ForbiddenError(App.Resources.Views.UiText.AccessDeniedDescription));
        }

        var canDelete = await _deleteGuard.CanDeleteCustomerAsync(
            access.Value.CustomerId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (!canDelete)
        {
            return Result.Fail(new BusinessRuleError(DeleteBlockedMessage()));
        }

        await _uow.Customers.RemoveAsync(
            access.Value.CustomerId,
            access.Value.ManagementCompanyId,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    private async Task<Result<CustomerAccessContext>> ResolveAccessAsync(
        Guid appUserId,
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken)
    {
        if (appUserId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authentication is required."));
        }

        var access = await _customerAccessService.ResolveCustomerWorkspaceAsync(
            new GetCustomerWorkspaceQuery
            {
                UserId = appUserId,
                CompanySlug = companySlug,
                CustomerSlug = customerSlug
            },
            cancellationToken);

        if (access.IsFailed)
        {
            return Result.Fail(access.Errors);
        }

        return Result.Ok(new CustomerAccessContext(
            access.Value.ManagementCompanyId,
            access.Value.CustomerId));
    }

    private static Result ValidateUpdate(UpdateCustomerProfileCommand command)
    {
        var failures = new List<ValidationFailureModel>();

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(command.Name),
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.Name)
            });
        }

        if (string.IsNullOrWhiteSpace(command.RegistryCode))
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(command.RegistryCode),
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.RegistryCode)
            });
        }

        var normalizedBillingEmail = string.IsNullOrWhiteSpace(command.BillingEmail)
            ? null
            : command.BillingEmail.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedBillingEmail) &&
            !new EmailAddressAttribute().IsValid(normalizedBillingEmail))
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(command.BillingEmail),
                ErrorMessage = App.Resources.Views.UiText.InvalidEmailAddress
            });
        }

        return failures.Count == 0
            ? Result.Ok()
            : Result.Fail(new ValidationAppError("Validation failed.", failures));
    }

    private static NormalizedUpdate NormalizeUpdate(UpdateCustomerProfileCommand command)
    {
        return new NormalizedUpdate(
            command.Name.Trim(),
            command.RegistryCode.Trim(),
            string.IsNullOrWhiteSpace(command.BillingEmail) ? null : command.BillingEmail.Trim(),
            string.IsNullOrWhiteSpace(command.BillingAddress) ? null : command.BillingAddress.Trim(),
            string.IsNullOrWhiteSpace(command.Phone) ? null : command.Phone.Trim());
    }

    private record CustomerAccessContext(Guid ManagementCompanyId, Guid CustomerId);

    private record NormalizedUpdate(
        string Name,
        string RegistryCode,
        string? BillingEmail,
        string? BillingAddress,
        string? Phone);

    private static string DeleteBlockedMessage()
    {
        return App.Resources.Views.UiText.ResourceManager.GetString("UnableToDeleteBecauseDependentRecordsExist")
               ?? "Unable to delete because dependent records exist.";
    }
}
