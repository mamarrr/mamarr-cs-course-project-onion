using App.BLL.Contracts.Common;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Customers;
using App.BLL.Contracts.Customers.Models;
using App.BLL.Contracts.Customers.Queries;
using App.BLL.Contracts.Properties;
using App.BLL.Contracts.Properties.Commands;
using App.BLL.Contracts.Properties.Models;
using App.BLL.Contracts.Properties.Queries;
using App.BLL.Mappers.Properties;
using App.BLL.Shared.Routing;
using App.DAL.Contracts;
using App.DAL.DTO.Properties;
using FluentResults;

namespace App.BLL.Services.Properties;

public class PropertyWorkspaceService : IPropertyWorkspaceService
{
    private readonly ICustomerAccessService _customerAccessService;
    private readonly IAppUOW _uow;

    public PropertyWorkspaceService(
        ICustomerAccessService customerAccessService,
        IAppUOW uow)
    {
        _customerAccessService = customerAccessService;
        _uow = uow;
    }

    public async Task<Result<PropertyWorkspaceModel>> GetWorkspaceAsync(
        GetPropertyWorkspaceQuery query,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(query.UserId, query.CompanySlug, query.CustomerSlug, cancellationToken);
        if (customer.IsFailed)
        {
            return Result.Fail(customer.Errors);
        }

        if (string.IsNullOrWhiteSpace(query.PropertySlug))
        {
            return Result.Fail(new NotFoundError("Property context was not found."));
        }

        var property = await _uow.Properties.FirstWorkspaceByCustomerAndSlugAsync(
            customer.Value.CustomerId,
            query.PropertySlug,
            cancellationToken);

        return property is null
            ? Result.Fail(new NotFoundError("Property context was not found."))
            : Result.Ok(PropertyBllMapper.MapWorkspace(customer.Value, property));
    }

    public async Task<Result<PropertyDashboardModel>> GetDashboardAsync(
        GetPropertyWorkspaceQuery query,
        CancellationToken cancellationToken = default)
    {
        var workspace = await GetWorkspaceAsync(query, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        return Result.Ok(new PropertyDashboardModel
        {
            Workspace = workspace.Value
        });
    }

    public async Task<Result<IReadOnlyList<PropertyListItemModel>>> GetCustomerPropertiesAsync(
        GetPropertyWorkspaceQuery query,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(query.UserId, query.CompanySlug, query.CustomerSlug, cancellationToken);
        if (customer.IsFailed)
        {
            return Result.Fail(customer.Errors);
        }

        var properties = await _uow.Properties.AllByCustomerAsync(
            customer.Value.CustomerId,
            cancellationToken);

        return Result.Ok((IReadOnlyList<PropertyListItemModel>)properties
            .Select(PropertyBllMapper.MapListItem)
            .ToList());
    }

    public async Task<Result<IReadOnlyList<PropertyTypeOptionModel>>> GetPropertyTypeOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var options = await _uow.Lookups.AllPropertyTypeOptionsAsync(cancellationToken);

        return Result.Ok((IReadOnlyList<PropertyTypeOptionModel>)options
            .Select(PropertyBllMapper.MapTypeOption)
            .ToList());
    }

    public async Task<Result<PropertyProfileModel>> CreateAsync(
        CreatePropertyCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateCreate(command);
        if (validation.IsFailed)
        {
            return Result.Fail(validation.Errors);
        }

        var customer = await ResolveCustomerAsync(command.UserId, command.CompanySlug, command.CustomerSlug, cancellationToken);
        if (customer.IsFailed)
        {
            return Result.Fail(customer.Errors);
        }

        var propertyTypeExists = await _uow.Lookups.PropertyTypeExistsAsync(
            command.PropertyTypeId,
            cancellationToken);
        if (!propertyTypeExists)
        {
            return Result.Fail(new ValidationAppError(
                App.Resources.Views.UiText.ResourceManager.GetString("InvalidPropertyType") ?? "Selected property type is invalid.",
                [
                    new ValidationFailureModel
                    {
                        PropertyName = nameof(command.PropertyTypeId),
                        ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("InvalidPropertyType") ?? "Selected property type is invalid."
                    }
                ]));
        }

        var normalized = NormalizeCreate(command);
        var properties = await _uow.Properties.AllByCustomerAsync(customer.Value.CustomerId, cancellationToken);
        var slug = SlugGenerator.EnsureUniqueSlug(
            SlugGenerator.GenerateSlug(normalized.Name),
            properties.Select(property => property.Slug));

        var createDto = new PropertyDalDto
        {
            CustomerId = customer.Value.CustomerId,
            Label = normalized.Name,
            Slug = slug,
            AddressLine = normalized.AddressLine,
            City = normalized.City,
            PostalCode = normalized.PostalCode,
            PropertyTypeId = command.PropertyTypeId,
            Notes = normalized.Notes,
        };

        var propertyId = _uow.Properties.Add(createDto);
        await _uow.SaveChangesAsync(cancellationToken);

        var profile = await _uow.Properties.FindProfileAsync(
            propertyId,
            customer.Value.CustomerId,
            cancellationToken);

        return profile is null
            ? Result.Fail(new NotFoundError("Property profile was not found."))
            : Result.Ok(PropertyBllMapper.MapProfile(profile));
    }

    private async Task<Result<CustomerWorkspaceModel>> ResolveCustomerAsync(
        Guid userId,
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authentication is required."));
        }

        return await _customerAccessService.ResolveCustomerWorkspaceAsync(
            new GetCustomerWorkspaceQuery
            {
                UserId = userId,
                CompanySlug = companySlug,
                CustomerSlug = customerSlug
            },
            cancellationToken);
    }

    private static Result ValidateCreate(CreatePropertyCommand command)
    {
        var failures = new List<ValidationFailureModel>();

        AddRequired(failures, nameof(command.Name), command.Name, App.Resources.Views.UiText.Name);
        AddRequired(failures, nameof(command.AddressLine), command.AddressLine, App.Resources.Views.UiText.AddressLine);
        AddRequired(failures, nameof(command.City), command.City, App.Resources.Views.UiText.City);
        AddRequired(failures, nameof(command.PostalCode), command.PostalCode, App.Resources.Views.UiText.PostalCode);

        if (command.PropertyTypeId == Guid.Empty)
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(command.PropertyTypeId),
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", "Property type")
            });
        }

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

    private static NormalizedCreate NormalizeCreate(CreatePropertyCommand command)
    {
        return new NormalizedCreate(
            command.Name.Trim(),
            command.AddressLine.Trim(),
            command.City.Trim(),
            command.PostalCode.Trim(),
            string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim());
    }

    private sealed record NormalizedCreate(
        string Name,
        string AddressLine,
        string City,
        string PostalCode,
        string? Notes);
}
