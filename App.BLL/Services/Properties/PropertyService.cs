using App.BLL.Contracts.Common.Portal;
using App.BLL.Contracts.Properties;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Customers.Models;
using App.BLL.DTO.Properties;
using App.BLL.DTO.Properties.Models;
using App.BLL.Mappers.Properties;
using App.BLL.Shared.Routing;
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

    private static readonly HashSet<string> AccessAllowedRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER",
        "FINANCE",
        "SUPPORT"
    };

    private readonly IPortalContextProvider _portalContext;

    public PropertyService(
        IAppUOW uow,
        IPortalContextProvider portalContext)
        : base(uow.Properties, uow, new PropertyBllDtoMapper())
    {
        _portalContext = portalContext;
    }

    public async Task<Result<PropertyWorkspaceModel>> ResolveWorkspaceAsync(
        PropertyRoute route,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerWorkspaceAsync(route.AppUserId, route.CompanySlug, route.CustomerSlug, cancellationToken);
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
            : Result.Ok(new PropertyWorkspaceModel
            {
                AppUserId = customer.Value.AppUserId,
                ManagementCompanyId = customer.Value.ManagementCompanyId,
                CompanySlug = customer.Value.CompanySlug,
                CompanyName = customer.Value.CompanyName,
                CustomerId = customer.Value.CustomerId,
                CustomerSlug = customer.Value.CustomerSlug,
                CustomerName = customer.Value.CustomerName,
                PropertyId = property.Id,
                PropertySlug = property.Slug,
                PropertyName = property.Name
            });
    }

    public async Task<Result<PropertyDashboardModel>> GetDashboardAsync(
        PropertyRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveWorkspaceAsync(route, cancellationToken);
        return workspace.IsFailed
            ? Result.Fail<PropertyDashboardModel>(workspace.Errors)
            : Result.Ok(new PropertyDashboardModel { Workspace = workspace.Value });
    }

    public async Task<Result<IReadOnlyList<PropertyListItemModel>>> ListForCustomerAsync(
        CustomerRoute route,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerWorkspaceAsync(route.AppUserId, route.CompanySlug, route.CustomerSlug, cancellationToken);
        if (customer.IsFailed)
        {
            return Result.Fail(customer.Errors);
        }

        var properties = await ServiceUOW.Properties.AllByCustomerAsync(
            customer.Value.CustomerId,
            cancellationToken);

        return Result.Ok((IReadOnlyList<PropertyListItemModel>)properties
            .Select(property => new PropertyListItemModel
            {
                PropertyId = property.Id,
                CustomerId = property.CustomerId,
                ManagementCompanyId = property.ManagementCompanyId,
                PropertyName = property.Name,
                PropertySlug = property.Slug,
                AddressLine = property.AddressLine,
                City = property.City,
                PostalCode = property.PostalCode,
                PropertyTypeId = property.PropertyTypeId,
                PropertyTypeCode = property.PropertyTypeCode,
                PropertyTypeLabel = property.PropertyTypeLabel
            })
            .ToList());
    }

    public async Task<Result<PropertyProfileModel>> GetProfileAsync(
        PropertyRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveWorkspaceAsync(route, cancellationToken);
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
            .Select(option => new PropertyTypeOptionModel
            {
                Id = option.Id,
                Code = option.Code,
                Label = option.Label
            })
            .ToList());
    }

    public async Task<Result<PropertyBllDto>> CreateAsync(
        CustomerRoute route,
        PropertyBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerWorkspaceAsync(route.AppUserId, route.CompanySlug, route.CustomerSlug, cancellationToken);
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
        var workspace = await ResolveWorkspaceAsync(route, cancellationToken);
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
        var workspace = await ResolveWorkspaceAsync(route, cancellationToken);
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

        var hasDependencies = await ServiceUOW.Properties.HasDeleteDependenciesAsync(
            workspace.Value.PropertyId,
            workspace.Value.CustomerId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (hasDependencies)
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

    private async Task<Result<CustomerWorkspaceModel>> ResolveCustomerWorkspaceAsync(
        Guid userId,
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken)
    {
        return await _portalContext.ResolveCustomerWorkspaceAsync(
            new CustomerRoute
            {
                AppUserId = userId,
                CompanySlug = companySlug,
                CustomerSlug = customerSlug
            },
            AccessAllowedRoleCodes,
            allowCustomerContext: true,
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
            : Result.Ok(new PropertyProfileModel
            {
                PropertyId = profile.Id,
                CustomerId = profile.CustomerId,
                ManagementCompanyId = profile.ManagementCompanyId,
                CompanySlug = profile.CompanySlug,
                CompanyName = profile.CompanyName,
                CustomerSlug = profile.CustomerSlug,
                CustomerName = profile.CustomerName,
                PropertySlug = profile.Slug,
                Name = profile.Name,
                AddressLine = profile.AddressLine,
                City = profile.City,
                PostalCode = profile.PostalCode,
                Notes = profile.Notes,
                PropertyTypeId = profile.PropertyTypeId,
                PropertyTypeCode = profile.PropertyTypeCode,
                PropertyTypeLabel = profile.PropertyTypeLabel
            });
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
