using App.BLL.Contracts.Common.Deletion;
using App.BLL.Contracts.Customers;
using App.BLL.Contracts.Properties;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Customers.Models;
using App.BLL.DTO.Properties;
using App.BLL.DTO.Properties.Models;
using App.BLL.Shared.Routing;
using App.BLL.Mappers.Properties;
using App.DAL.Contracts;
using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Properties;
using Base.BLL;
using FluentResults;

namespace App.BLL.Services.Properties;

public class PropertyService :
    BaseService<PropertyBllDto, PropertyDalDto, IPropertyRepository, IAppUOW>,
    IPropertyService
{
    private static readonly HashSet<string> DeleteAllowedRoleCodes =
    [
        "OWNER",
        "MANAGER"
    ];

    private readonly ICustomerService _customerService;
    private readonly IAppDeleteGuard _deleteGuard;

    public PropertyService(
        IAppUOW uow,
        ICustomerService customerService,
        IAppDeleteGuard deleteGuard)
        : base(uow.Properties, uow, new PropertyBllDtoMapper())
    {
        _customerService = customerService;
        _deleteGuard = deleteGuard;
    }

    public async Task<Result<PropertyWorkspaceModel>> GetWorkspaceAsync(
        PropertyRoute route,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(route.AppUserId, route.CompanySlug, route.CustomerSlug, cancellationToken);
        if (customer.IsFailed)
        {
            return Result.Fail(customer.Errors);
        }

        var property = await ServiceUOW.Properties.FirstWorkspaceByCustomerAndSlugAsync(
            customer.Value.CustomerId,
            route.PropertySlug,
            cancellationToken);

        return property is null
            ? Result.Fail(new NotFoundError("Property context was not found."))
            : Result.Ok(PropertyBllMapper.MapWorkspace(customer.Value, property));
    }

    public async Task<Result<PropertyDashboardModel>> GetDashboardAsync(
        PropertyRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await GetWorkspaceAsync(route, cancellationToken);
        return workspace.IsFailed
            ? Result.Fail<PropertyDashboardModel>(workspace.Errors)
            : Result.Ok(new PropertyDashboardModel { Workspace = workspace.Value });
    }

    public async Task<Result<IReadOnlyList<PropertyListItemModel>>> ListForCustomerAsync(
        CustomerRoute route,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(route.AppUserId, route.CompanySlug, route.CustomerSlug, cancellationToken);
        if (customer.IsFailed)
        {
            return Result.Fail(customer.Errors);
        }

        var properties = await ServiceUOW.Properties.AllByCustomerAsync(
            customer.Value.CustomerId,
            cancellationToken);

        return Result.Ok((IReadOnlyList<PropertyListItemModel>)properties
            .Select(PropertyBllMapper.MapListItem)
            .ToList());
    }

    public async Task<Result<PropertyProfileModel>> GetProfileAsync(
        PropertyRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await GetWorkspaceAsync(route, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        return await GetProfileAsync(workspace.Value, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<PropertyTypeOptionModel>>> GetPropertyTypeOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var options = await ServiceUOW.Lookups.AllPropertyTypeOptionsAsync(cancellationToken);

        return Result.Ok((IReadOnlyList<PropertyTypeOptionModel>)options
            .Select(PropertyBllMapper.MapTypeOption)
            .ToList());
    }

    public async Task<Result<PropertyBllDto>> CreateAsync(
        CustomerRoute route,
        PropertyBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerAsync(route.AppUserId, route.CompanySlug, route.CustomerSlug, cancellationToken);
        if (customer.IsFailed)
        {
            return Result.Fail(customer.Errors);
        }

        var validation = Validate(dto, requirePropertyType: true);
        if (validation.IsFailed)
        {
            return Result.Fail<PropertyBllDto>(validation.Errors);
        }

        var propertyTypeExists = await ServiceUOW.Lookups.PropertyTypeExistsAsync(
            dto.PropertyTypeId,
            cancellationToken);
        if (!propertyTypeExists)
        {
            return Result.Fail(new ValidationAppError(
                App.Resources.Views.UiText.ResourceManager.GetString("InvalidPropertyType") ?? "Selected property type is invalid.",
                [
                    new ValidationFailureModel
                    {
                        PropertyName = nameof(dto.PropertyTypeId),
                        ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("InvalidPropertyType") ?? "Selected property type is invalid."
                    }
                ]));
        }

        var normalized = Normalize(dto);
        var properties = await ServiceUOW.Properties.AllByCustomerAsync(customer.Value.CustomerId, cancellationToken);

        dto.Id = Guid.Empty;
        dto.CustomerId = customer.Value.CustomerId;
        dto.Label = normalized.Name;
        dto.Slug = SlugGenerator.EnsureUniqueSlug(
            SlugGenerator.GenerateSlug(normalized.Name),
            properties.Select(property => property.Slug));
        dto.AddressLine = normalized.AddressLine;
        dto.City = normalized.City;
        dto.PostalCode = normalized.PostalCode;
        dto.Notes = normalized.Notes;

        return await AddAndFindCoreAsync(dto, customer.Value.CustomerId, cancellationToken);
    }

    public async Task<Result<PropertyProfileModel>> CreateAndGetProfileAsync(
        CustomerRoute route,
        PropertyBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var created = await CreateAsync(route, dto, cancellationToken);
        if (created.IsFailed)
        {
            return Result.Fail<PropertyProfileModel>(created.Errors);
        }

        return await GetProfileAsync(
            new PropertyRoute
            {
                AppUserId = route.AppUserId,
                CompanySlug = route.CompanySlug,
                CustomerSlug = route.CustomerSlug,
                PropertySlug = created.Value.Slug
            },
            cancellationToken);
    }

    public async Task<Result<PropertyBllDto>> UpdateAsync(
        PropertyRoute route,
        PropertyBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var workspace = await GetWorkspaceAsync(route, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var profile = await ServiceUOW.Properties.FindProfileAsync(
            workspace.Value.PropertyId,
            workspace.Value.CustomerId,
            cancellationToken);
        if (profile is null)
        {
            return Result.Fail(new NotFoundError("Property profile was not found."));
        }

        var validation = Validate(dto, requirePropertyType: false);
        if (validation.IsFailed)
        {
            return Result.Fail<PropertyBllDto>(validation.Errors);
        }

        var normalized = Normalize(dto);
        dto.Id = workspace.Value.PropertyId;
        dto.CustomerId = workspace.Value.CustomerId;
        dto.PropertyTypeId = profile.PropertyTypeId;
        dto.Label = normalized.Name;
        dto.Slug = profile.Slug;
        dto.AddressLine = normalized.AddressLine;
        dto.City = normalized.City;
        dto.PostalCode = normalized.PostalCode;
        dto.Notes = normalized.Notes;

        var updated = await base.UpdateAsync(dto, workspace.Value.CustomerId, cancellationToken);
        if (updated.IsFailed)
        {
            return Result.Fail<PropertyBllDto>(updated.Errors);
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return updated;
    }

    public async Task<Result<PropertyProfileModel>> UpdateAndGetProfileAsync(
        PropertyRoute route,
        PropertyBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var updated = await UpdateAsync(route, dto, cancellationToken);
        return updated.IsFailed
            ? Result.Fail<PropertyProfileModel>(updated.Errors)
            : await GetProfileAsync(route, cancellationToken);
    }

    public async Task<Result> DeleteAsync(
        PropertyRoute route,
        string confirmationName,
        CancellationToken cancellationToken = default)
    {
        var workspace = await GetWorkspaceAsync(route, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var profile = await ServiceUOW.Properties.FindProfileAsync(
            workspace.Value.PropertyId,
            workspace.Value.CustomerId,
            cancellationToken);
        if (profile is null)
        {
            return Result.Fail(new NotFoundError("Property profile was not found."));
        }

        if (!string.Equals(confirmationName?.Trim(), profile.Name.Trim(), StringComparison.Ordinal))
        {
            return Result.Fail(new ValidationAppError(
                "Delete confirmation does not match the current property name.",
                [
                    new ValidationFailureModel
                    {
                        PropertyName = "ConfirmationName",
                        ErrorMessage = "Delete confirmation does not match the current property name."
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

        var canDelete = await _deleteGuard.CanDeletePropertyAsync(
            workspace.Value.PropertyId,
            workspace.Value.CustomerId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (!canDelete)
        {
            return Result.Fail(new BusinessRuleError(DeleteBlockedMessage()));
        }

        var removed = await base.RemoveAsync(workspace.Value.PropertyId, workspace.Value.CustomerId, cancellationToken);
        if (removed.IsFailed)
        {
            return Result.Fail(removed.Errors);
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return Result.Ok();
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

        return await _customerService.GetWorkspaceAsync(
            new CustomerRoute
            {
                AppUserId = userId,
                CompanySlug = companySlug,
                CustomerSlug = customerSlug
            },
            cancellationToken);
    }

    private async Task<Result<PropertyProfileModel>> GetProfileAsync(
        PropertyWorkspaceModel workspace,
        CancellationToken cancellationToken)
    {
        var profile = await ServiceUOW.Properties.FindProfileAsync(
            workspace.PropertyId,
            workspace.CustomerId,
            cancellationToken);

        return profile is null
            ? Result.Fail(new NotFoundError("Property profile was not found."))
            : Result.Ok(PropertyBllMapper.MapProfile(profile));
    }

    private static Result Validate(PropertyBllDto dto, bool requirePropertyType)
    {
        var failures = new List<ValidationFailureModel>();

        AddRequired(failures, nameof(dto.Label), dto.Label, App.Resources.Views.UiText.Name);
        AddRequired(failures, nameof(dto.AddressLine), dto.AddressLine, App.Resources.Views.UiText.AddressLine);
        AddRequired(failures, nameof(dto.City), dto.City, App.Resources.Views.UiText.City);
        AddRequired(failures, nameof(dto.PostalCode), dto.PostalCode, App.Resources.Views.UiText.PostalCode);

        if (requirePropertyType && dto.PropertyTypeId == Guid.Empty)
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(dto.PropertyTypeId),
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

    private static NormalizedProperty Normalize(PropertyBllDto dto)
    {
        return new NormalizedProperty(
            dto.Label.Trim(),
            dto.AddressLine.Trim(),
            dto.City.Trim(),
            dto.PostalCode.Trim(),
            string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim());
    }

    private sealed record NormalizedProperty(
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
