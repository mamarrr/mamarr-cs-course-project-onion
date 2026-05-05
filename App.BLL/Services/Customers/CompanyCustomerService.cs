using System.ComponentModel.DataAnnotations;
using App.BLL.Contracts.Common;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Customers;
using App.BLL.Contracts.Customers.Commands;
using App.BLL.Contracts.Customers.Errors;
using App.BLL.Contracts.Customers.Models;
using App.BLL.Contracts.Customers.Queries;
using App.BLL.Mappers.Customers;
using App.BLL.Shared.Routing;
using App.DAL.Contracts;
using App.DAL.DTO.Customers;
using FluentResults;

namespace App.BLL.Services.Customers;

public class CompanyCustomerService : ICompanyCustomerService
{
    private readonly ICustomerAccessService _customerAccessService;
    private readonly IAppUOW _uow;

    public CompanyCustomerService(
        ICustomerAccessService customerAccessService,
        IAppUOW uow)
    {
        _customerAccessService = customerAccessService;
        _uow = uow;
    }

    public async Task<Result<IReadOnlyList<CustomerListItemModel>>> GetCompanyCustomersAsync(
        GetCompanyCustomersQuery query,
        CancellationToken cancellationToken = default)
    {
        var access = await _customerAccessService.ResolveCompanyWorkspaceAsync(query, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail(access.Errors);
        }

        var customers = await _uow.Customers.AllByCompanyIdAsync(
            access.Value.ManagementCompanyId,
            cancellationToken);

        var propertyLinks = await _uow.Customers.AllPropertyLinksByCompanyIdAsync(
            access.Value.ManagementCompanyId,
            cancellationToken);

        var linksByCustomerId = propertyLinks
            .GroupBy(link => link.CustomerId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<CustomerPropertyLinkModel>)group
                    .Select(link => new CustomerPropertyLinkModel
                    {
                        PropertySlug = link.PropertySlug,
                        PropertyName = link.PropertyName
                    })
                    .ToList());

        return Result.Ok((IReadOnlyList<CustomerListItemModel>)customers
            .Select(customer => CustomerWorkspaceBllMapper.MapListItem(
                customer,
                access.Value,
                linksByCustomerId.GetValueOrDefault(customer.Id, Array.Empty<CustomerPropertyLinkModel>())))
            .ToList());
    }

    public async Task<Result<CompanyCustomerModel>> CreateCustomerAsync(
        CreateCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(command);
        if (validation.IsFailed)
        {
            return Result.Fail(validation.Errors);
        }

        var access = await _customerAccessService.ResolveCompanyWorkspaceAsync(
            new GetCompanyCustomersQuery
            {
                UserId = command.UserId,
                CompanySlug = command.CompanySlug
            },
            cancellationToken);

        if (access.IsFailed)
        {
            return Result.Fail(access.Errors);
        }

        var normalized = Normalize(command);

        var duplicateRegistryCode = await _uow.Customers.RegistryCodeExistsInCompanyAsync(
            access.Value.ManagementCompanyId,
            normalized.RegistryCode,
            cancellationToken: cancellationToken);

        if (duplicateRegistryCode)
        {
            return Result.Fail(new DuplicateRegistryCodeError(
                App.Resources.Views.UiText.ResourceManager.GetString("CustomerRegistryCodeAlreadyExists")
                ?? "Customer with this registry code already exists in this company.",
                nameof(command.RegistryCode)));
        }

        var customers = await _uow.Customers.AllByCompanyIdAsync(
            access.Value.ManagementCompanyId,
            cancellationToken);

        var slug = SlugGenerator.EnsureUniqueSlug(
            SlugGenerator.GenerateSlug(normalized.Name),
            customers.Select(customer => customer.Slug));

        var createDto = new CustomerCreateDalDto
        {
            ManagementCompanyId = access.Value.ManagementCompanyId,
            Name = normalized.Name,
            Slug = slug,
            RegistryCode = normalized.RegistryCode,
            BillingEmail = normalized.BillingEmail,
            BillingAddress = normalized.BillingAddress,
            Phone = normalized.Phone,
        };

        var created = await _uow.Customers.AddAsync(createDto, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Ok(CustomerWorkspaceBllMapper.MapCreated(created, access.Value, createDto));
    }

    private static Result Validate(CreateCustomerCommand command)
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

    private static NormalizedCustomerCreate Normalize(CreateCustomerCommand command)
    {
        return new NormalizedCustomerCreate(
            command.Name.Trim(),
            command.RegistryCode.Trim(),
            string.IsNullOrWhiteSpace(command.BillingEmail) ? null : command.BillingEmail.Trim(),
            string.IsNullOrWhiteSpace(command.BillingAddress) ? null : command.BillingAddress.Trim(),
            string.IsNullOrWhiteSpace(command.Phone) ? null : command.Phone.Trim());
    }

    private sealed record NormalizedCustomerCreate(
        string Name,
        string RegistryCode,
        string? BillingEmail,
        string? BillingAddress,
        string? Phone);
}
