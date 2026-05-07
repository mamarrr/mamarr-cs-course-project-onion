using System.ComponentModel.DataAnnotations;
using App.BLL.Contracts.Common.Deletion;
using App.BLL.Contracts.Customers;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Customers;
using App.BLL.DTO.Customers.Errors;
using App.BLL.DTO.Customers.Models;
using App.BLL.Mappers.Customers;
using App.DAL.Contracts;
using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Customers;
using App.BLL.Shared.Routing;
using Base.BLL;
using FluentResults;

namespace App.BLL.Services.Customers;

public class CustomerService :
    BaseService<CustomerBllDto, CustomerDalDto, ICustomerRepository, IAppUOW>,
    ICustomerService
{
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

    private readonly IAppDeleteGuard _deleteGuard;

    public CustomerService(
        IAppUOW uow,
        IAppDeleteGuard deleteGuard)
        : base(uow.Customers, uow, new CustomerBllDtoMapper())
    {
        _deleteGuard = deleteGuard;
    }

    public async Task<Result<CompanyWorkspaceModel>> ResolveCompanyWorkspaceAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default)
    {
        if (route.AppUserId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authentication is required."));
        }

        var company = await ServiceUOW.ManagementCompanies.FirstBySlugAsync(route.CompanySlug, cancellationToken);
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
            return Result.Fail(new ForbiddenError(App.Resources.Views.UiText.AccessDeniedDescription));
        }

        return Result.Ok(new CompanyWorkspaceModel
        {
            AppUserId = route.AppUserId,
            ManagementCompanyId = company.Id,
            CompanySlug = company.Slug,
            CompanyName = company.Name
        });
    }

    public async Task<Result<IReadOnlyList<CustomerListItemModel>>> ListForCompanyAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveCompanyWorkspaceAsync(route, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail(access.Errors);
        }

        var customers = await ServiceUOW.Customers.AllByCompanyIdAsync(
            access.Value.ManagementCompanyId,
            cancellationToken);

        return Result.Ok((IReadOnlyList<CustomerListItemModel>)customers
            .Select(customer => new CustomerListItemModel
            {
                CustomerId = customer.Id,
                ManagementCompanyId = customer.ManagementCompanyId,
                CompanySlug = access.Value.CompanySlug,
                CompanyName = access.Value.CompanyName,
                CustomerSlug = customer.Slug,
                Name = customer.Name,
                RegistryCode = customer.RegistryCode,
                BillingEmail = customer.BillingEmail,
                BillingAddress = customer.BillingAddress,
                Phone = customer.Phone
            })
            .ToList());
    }

    public async Task<Result<CustomerWorkspaceModel>> ResolveWorkspaceAsync(
        CustomerRoute route,
        CancellationToken cancellationToken = default)
    {
        if (route.AppUserId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authentication is required."));
        }

        var company = await ServiceUOW.ManagementCompanies.FirstBySlugAsync(route.CompanySlug, cancellationToken);
        if (company is null)
        {
            return Result.Fail(new NotFoundError(App.Resources.Views.UiText.ManagementCompanyWasNotFound));
        }

        if (string.IsNullOrWhiteSpace(route.CustomerSlug))
        {
            return Result.Fail(new NotFoundError("Customer context was not found."));
        }

        var customer = await ServiceUOW.Customers.FirstWorkspaceByCompanyAndSlugAsync(
            company.Id,
            route.CustomerSlug,
            cancellationToken);

        if (customer is null)
        {
            return Result.Fail(new NotFoundError("Customer context was not found."));
        }

        var roleCode = await ServiceUOW.ManagementCompanies.FindActiveUserRoleCodeAsync(
            route.AppUserId,
            company.Id,
            cancellationToken);
        if (roleCode is not null && AccessAllowedRoleCodes.Contains(roleCode))
        {
            return Result.Ok(new CustomerWorkspaceModel
            {
                AppUserId = route.AppUserId,
                ManagementCompanyId = customer.ManagementCompanyId,
                CompanySlug = customer.CompanySlug,
                CompanyName = customer.CompanyName,
                CustomerId = customer.Id,
                CustomerSlug = customer.Slug,
                CustomerName = customer.Name
            });
        }

        var hasCustomerContext = await ServiceUOW.Customers.ActiveUserCustomerContextExistsAsync(
            route.AppUserId,
            customer.Id,
            cancellationToken);

        return hasCustomerContext
            ? Result.Ok(new CustomerWorkspaceModel
            {
                AppUserId = route.AppUserId,
                ManagementCompanyId = customer.ManagementCompanyId,
                CompanySlug = customer.CompanySlug,
                CompanyName = customer.CompanyName,
                CustomerId = customer.Id,
                CustomerSlug = customer.Slug,
                CustomerName = customer.Name
            })
            : Result.Fail(new ForbiddenError(App.Resources.Views.UiText.AccessDeniedDescription));
    }

    public async Task<Result<CustomerProfileModel>> GetProfileAsync(
        CustomerRoute route,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveAccessAsync(route, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail(access.Errors);
        }

        var profile = await ServiceUOW.Customers.FindProfileAsync(
            access.Value.CustomerId,
            access.Value.ManagementCompanyId,
            cancellationToken);

        return profile is null
            ? Result.Fail(new NotFoundError("Customer profile was not found."))
            : Result.Ok(new CustomerProfileModel
            {
                Id = profile.Id,
                ManagementCompanyId = profile.ManagementCompanyId,
                CompanySlug = profile.CompanySlug,
                CompanyName = profile.CompanyName,
                Name = profile.Name,
                Slug = profile.Slug,
                RegistryCode = profile.RegistryCode,
                BillingEmail = profile.BillingEmail,
                BillingAddress = profile.BillingAddress,
                Phone = profile.Phone
            });
    }

    public async Task<Result<CustomerBllDto>> CreateAsync(
        ManagementCompanyRoute route,
        CustomerBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveCompanyWorkspaceAsync(route, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail(access.Errors);
        }

        var validation = Validate(dto);
        if (validation.IsFailed)
        {
            return Result.Fail<CustomerBllDto>(validation.Errors);
        }

        var normalized = Normalize(dto);
        var duplicateRegistryCode = await ServiceUOW.Customers.RegistryCodeExistsInCompanyAsync(
            access.Value.ManagementCompanyId,
            normalized.RegistryCode,
            cancellationToken: cancellationToken);
        if (duplicateRegistryCode)
        {
            return Result.Fail(new DuplicateRegistryCodeError(
                App.Resources.Views.UiText.ResourceManager.GetString("CustomerRegistryCodeAlreadyExists")
                ?? "Customer with this registry code already exists in this company.",
                nameof(dto.RegistryCode)));
        }

        var customers = await ServiceUOW.Customers.AllByCompanyIdAsync(
            access.Value.ManagementCompanyId,
            cancellationToken);

        dto.Id = Guid.Empty;
        dto.ManagementCompanyId = access.Value.ManagementCompanyId;
        dto.Name = normalized.Name;
        dto.Slug = SlugGenerator.EnsureUniqueSlug(
            SlugGenerator.GenerateSlug(normalized.Name),
            customers.Select(customer => customer.Slug));
        dto.RegistryCode = normalized.RegistryCode;
        dto.BillingEmail = normalized.BillingEmail;
        dto.BillingAddress = normalized.BillingAddress;
        dto.Phone = normalized.Phone;

        return await AddAndFindCoreAsync(dto, access.Value.ManagementCompanyId, cancellationToken);
    }

    public async Task<Result<CustomerProfileModel>> CreateAndGetProfileAsync(
        ManagementCompanyRoute route,
        CustomerBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var created = await CreateAsync(route, dto, cancellationToken);
        if (created.IsFailed)
        {
            return Result.Fail<CustomerProfileModel>(created.Errors);
        }

        return await GetProfileAsync(
            new CustomerRoute
            {
                AppUserId = route.AppUserId,
                CompanySlug = route.CompanySlug,
                CustomerSlug = created.Value.Slug
            },
            cancellationToken);
    }

    public async Task<Result<CustomerBllDto>> UpdateAsync(
        CustomerRoute route,
        CustomerBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveAccessAsync(route, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail(access.Errors);
        }

        var profile = await ServiceUOW.Customers.FindProfileAsync(
            access.Value.CustomerId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (profile is null)
        {
            return Result.Fail(new NotFoundError("Customer profile was not found."));
        }

        var validation = Validate(dto);
        if (validation.IsFailed)
        {
            return Result.Fail<CustomerBllDto>(validation.Errors);
        }

        var normalized = Normalize(dto);
        var duplicateRegistryCode = await ServiceUOW.Customers.RegistryCodeExistsInCompanyAsync(
            access.Value.ManagementCompanyId,
            normalized.RegistryCode,
            access.Value.CustomerId,
            cancellationToken);
        if (duplicateRegistryCode)
        {
            return Result.Fail(new DuplicateRegistryCodeError(
                App.Resources.Views.UiText.ResourceManager.GetString("CustomerRegistryCodeAlreadyExists")
                ?? "Customer with this registry code already exists in this company.",
                nameof(dto.RegistryCode)));
        }

        dto.Id = access.Value.CustomerId;
        dto.ManagementCompanyId = access.Value.ManagementCompanyId;
        dto.Name = normalized.Name;
        dto.Slug = profile.Slug;
        dto.RegistryCode = normalized.RegistryCode;
        dto.BillingEmail = normalized.BillingEmail;
        dto.BillingAddress = normalized.BillingAddress;
        dto.Phone = normalized.Phone;

        var updated = await base.UpdateAsync(dto, access.Value.ManagementCompanyId, cancellationToken);
        if (updated.IsFailed)
        {
            return Result.Fail<CustomerBllDto>(updated.Errors);
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return updated;
    }

    public async Task<Result<CustomerProfileModel>> UpdateAndGetProfileAsync(
        CustomerRoute route,
        CustomerBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var updated = await UpdateAsync(route, dto, cancellationToken);
        return updated.IsFailed
            ? Result.Fail<CustomerProfileModel>(updated.Errors)
            : await GetProfileAsync(route, cancellationToken);
    }

    public async Task<Result> DeleteAsync(
        CustomerRoute route,
        string confirmationName,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveAccessAsync(route, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail(access.Errors);
        }

        var profile = await ServiceUOW.Customers.FindProfileAsync(
            access.Value.CustomerId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (profile is null)
        {
            return Result.Fail(new NotFoundError("Customer profile was not found."));
        }

        if (!string.Equals(confirmationName?.Trim(), profile.Name.Trim(), StringComparison.Ordinal))
        {
            return Result.Fail(new ValidationAppError(
                "Delete confirmation does not match the current customer name.",
                [
                    new ValidationFailureModel
                    {
                        PropertyName = "ConfirmationName",
                        ErrorMessage = "Delete confirmation does not match the current customer name."
                    }
                ]));
        }

        var roleCode = await ServiceUOW.Customers.FindActiveManagementCompanyRoleCodeAsync(
            access.Value.ManagementCompanyId,
            route.AppUserId,
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

        var removed = await base.RemoveAsync(access.Value.CustomerId, access.Value.ManagementCompanyId, cancellationToken);
        if (removed.IsFailed)
        {
            return Result.Fail(removed.Errors);
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    private async Task<Result<CustomerAccessContext>> ResolveAccessAsync(
        CustomerRoute route,
        CancellationToken cancellationToken)
    {
        if (route.AppUserId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authentication is required."));
        }

        var access = await ResolveWorkspaceAsync(
            route,
            cancellationToken);

        return access.IsFailed
            ? Result.Fail<CustomerAccessContext>(access.Errors)
            : Result.Ok(new CustomerAccessContext(access.Value.ManagementCompanyId, access.Value.CustomerId));
    }

    private static Result Validate(CustomerBllDto dto)
    {
        var failures = new List<ValidationFailureModel>();

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(dto.Name),
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.Name)
            });
        }

        if (string.IsNullOrWhiteSpace(dto.RegistryCode))
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(dto.RegistryCode),
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.RegistryCode)
            });
        }

        var normalizedBillingEmail = string.IsNullOrWhiteSpace(dto.BillingEmail)
            ? null
            : dto.BillingEmail.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedBillingEmail) &&
            !new EmailAddressAttribute().IsValid(normalizedBillingEmail))
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(dto.BillingEmail),
                ErrorMessage = App.Resources.Views.UiText.InvalidEmailAddress
            });
        }

        return failures.Count == 0
            ? Result.Ok()
            : Result.Fail(new ValidationAppError("Validation failed.", failures));
    }

    private static NormalizedCustomer Normalize(CustomerBllDto dto)
    {
        return new NormalizedCustomer(
            dto.Name.Trim(),
            dto.RegistryCode.Trim(),
            string.IsNullOrWhiteSpace(dto.BillingEmail) ? null : dto.BillingEmail.Trim(),
            string.IsNullOrWhiteSpace(dto.BillingAddress) ? null : dto.BillingAddress.Trim(),
            string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim());
    }

    private sealed record CustomerAccessContext(Guid ManagementCompanyId, Guid CustomerId);

    private sealed record NormalizedCustomer(
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
