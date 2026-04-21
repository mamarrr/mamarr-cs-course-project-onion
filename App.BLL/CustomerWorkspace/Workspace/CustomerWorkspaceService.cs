using System.ComponentModel.DataAnnotations;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.CustomerWorkspace.Customers;
using App.BLL.PropertyWorkspace.Properties;
using App.BLL.Shared.Routing;
using App.DAL.EF;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.CustomerWorkspace.Workspace;

public class CustomerWorkspaceService :
    ICustomerWorkspaceService,
    ICustomerAccessService,
    ICompanyCustomerService,
    IPropertyWorkspaceService
{
    private static readonly HashSet<string> AllowedRoleCodes =
    [
        "OWNER",
        "MANAGER",
        "FINANCE",
        "SUPPORT"
    ];

    private readonly AppDbContext _dbContext;

    public CustomerWorkspaceService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CustomerWorkspaceAuthorizationResult> AuthorizeAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = companySlug.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return new CustomerWorkspaceAuthorizationResult
            {
                CompanyNotFound = true,
                ErrorMessage = App.Resources.Views.UiText.ManagementCompanyWasNotFound
            };
        }

        var company = await _dbContext.ManagementCompanies
            .AsNoTracking()
            .Where(c => c.Slug == normalizedSlug)
            .Select(c => new { c.Id, c.Slug, c.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (company == null)
        {
            return new CustomerWorkspaceAuthorizationResult
            {
                CompanyNotFound = true,
                ErrorMessage = App.Resources.Views.UiText.ManagementCompanyWasNotFound
            };
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var membership = await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Include(x => x.ManagementCompanyRole)
            .FirstOrDefaultAsync(x => x.AppUserId == appUserId && x.ManagementCompanyId == company.Id, cancellationToken);

        if (membership == null ||
            !membership.IsActive ||
            membership.ValidFrom > today ||
            (membership.ValidTo.HasValue && membership.ValidTo.Value < today) ||
            !AllowedRoleCodes.Contains((membership.ManagementCompanyRole?.Code ?? string.Empty).ToUpperInvariant()))
        {
            return new CustomerWorkspaceAuthorizationResult
            {
                IsForbidden = true,
                ErrorMessage = App.Resources.Views.UiText.AccessDeniedDescription
            };
        }

        return new CustomerWorkspaceAuthorizationResult
        {
            IsAuthorized = true,
            Context = new CustomerWorkspaceContext
            {
                AppUserId = appUserId,
                ManagementCompanyId = company.Id,
                CompanySlug = company.Slug,
                CompanyName = company.Name
            }
        };
    }

    public async Task<CustomerDashboardAccessResult> ResolveDashboardAccessAsync(
        Guid appUserId,
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken = default)
    {
        return await AuthorizeCustomerContextAsync(appUserId, companySlug, customerSlug, cancellationToken);
    }

    public async Task<CustomerDashboardAccessResult> AuthorizeCustomerContextAsync(
        Guid appUserId,
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken = default)
    {
        var authResult = await AuthorizeAsync(appUserId, companySlug, cancellationToken);
        if (authResult.CompanyNotFound)
        {
            return new CustomerDashboardAccessResult
            {
                CompanyNotFound = true,
                ErrorMessage = authResult.ErrorMessage
            };
        }

        if (authResult.IsForbidden || authResult.Context == null)
        {
            return new CustomerDashboardAccessResult
            {
                IsForbidden = true,
                ErrorMessage = authResult.ErrorMessage
            };
        }

        var normalizedCustomerSlug = customerSlug.Trim();
        if (string.IsNullOrWhiteSpace(normalizedCustomerSlug))
        {
            return new CustomerDashboardAccessResult
            {
                CustomerNotFound = true
            };
        }

        var customer = await _dbContext.Customers
            .AsNoTracking()
            .Where(c => c.ManagementCompanyId == authResult.Context.ManagementCompanyId && c.Slug == normalizedCustomerSlug)
            .Select(c => new { c.Id, c.Slug, c.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (customer == null)
        {
            return new CustomerDashboardAccessResult
            {
                CustomerNotFound = true
            };
        }

        return new CustomerDashboardAccessResult
        {
            IsAuthorized = true,
            Context = new CustomerWorkspaceDashboardContext
            {
                AppUserId = authResult.Context.AppUserId,
                ManagementCompanyId = authResult.Context.ManagementCompanyId,
                CompanySlug = authResult.Context.CompanySlug,
                CompanyName = authResult.Context.CompanyName,
                CustomerId = customer.Id,
                CustomerSlug = customer.Slug,
                CustomerName = customer.Name
            }
        };
    }

    public async Task<CustomerPropertyListResult> ListPropertiesAsync(
        CustomerWorkspaceDashboardContext context,
        CancellationToken cancellationToken = default)
    {
        var properties = await _dbContext.Properties
            .AsNoTracking()
            .Where(p => p.CustomerId == context.CustomerId)
            .OrderBy(p => p.Label)
            .ThenBy(p => p.AddressLine)
            .Select(p => new CustomerPropertyListItem
            {
                PropertyId = p.Id,
                PropertySlug = p.Slug,
                PropertyName = p.Label.ToString(),
                AddressLine = p.AddressLine,
                City = p.City,
                PostalCode = p.PostalCode,
                PropertyTypeId = p.PropertyTypeId,
                PropertyTypeCode = p.PropertyType!.Code,
                PropertyTypeLabel = p.PropertyType!.Label.ToString(),
                IsActive = p.IsActive
            })
            .ToListAsync(cancellationToken);

        return new CustomerPropertyListResult
        {
            Properties = properties
        };
    }

    public async Task<PropertyCreateResult> CreatePropertyAsync(
        CustomerWorkspaceDashboardContext context,
        PropertyCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new PropertyCreateResult
            {
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.Name)
            };
        }

        if (string.IsNullOrWhiteSpace(request.AddressLine))
        {
            return new PropertyCreateResult
            {
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.Address)
            };
        }

        if (string.IsNullOrWhiteSpace(request.City))
        {
            return new PropertyCreateResult
            {
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace(
                    "{0}",
                    App.Resources.Views.UiText.ResourceManager.GetString("City") ?? "City")
            };
        }

        if (string.IsNullOrWhiteSpace(request.PostalCode))
        {
            return new PropertyCreateResult
            {
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace(
                    "{0}",
                    App.Resources.Views.UiText.ResourceManager.GetString("PostalCode") ?? "Postal code")
            };
        }

        var normalizedName = request.Name.Trim();
        var normalizedAddressLine = request.AddressLine.Trim();
        var normalizedCity = request.City.Trim();
        var normalizedPostalCode = request.PostalCode.Trim();
        var normalizedNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        var propertyType = await _dbContext.PropertyTypes
            .AsNoTracking()
            .Where(pt => pt.Id == request.PropertyTypeId)
            .Select(pt => new { pt.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (propertyType == null)
        {
            return new PropertyCreateResult
            {
                InvalidPropertyType = true,
                ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("InvalidData") ?? "Invalid data."
            };
        }

        var baseSlug = SlugGenerator.GenerateSlug(normalizedName);
        var existingSlugs = await _dbContext.Properties
            .AsNoTracking()
            .Where(p => p.CustomerId == context.CustomerId && p.Slug.StartsWith(baseSlug))
            .Select(p => p.Slug)
            .ToListAsync(cancellationToken);

        var uniqueSlug = SlugGenerator.EnsureUniqueSlug(baseSlug, existingSlugs);

        var property = new Property
        {
            Id = Guid.NewGuid(),
            Label = normalizedName,
            Slug = uniqueSlug,
            AddressLine = normalizedAddressLine,
            City = normalizedCity,
            PostalCode = normalizedPostalCode,
            Notes = normalizedNotes == null ? null : new Base.Domain.LangStr(normalizedNotes),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            PropertyTypeId = propertyType.Id,
            CustomerId = context.CustomerId
        };

        _dbContext.Properties.Add(property);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PropertyCreateResult
        {
            Success = true,
            CreatedPropertyId = property.Id,
            CreatedPropertySlug = property.Slug
        };
    }

    public async Task<PropertyDashboardAccessResult> ResolvePropertyDashboardContextAsync(
        CustomerWorkspaceDashboardContext context,
        string propertySlug,
        CancellationToken cancellationToken = default)
    {
        var normalizedPropertySlug = propertySlug.Trim();
        if (string.IsNullOrWhiteSpace(normalizedPropertySlug))
        {
            return new PropertyDashboardAccessResult
            {
                PropertyNotFound = true
            };
        }

        var property = await _dbContext.Properties
            .AsNoTracking()
            .Where(p => p.CustomerId == context.CustomerId && p.Slug == normalizedPropertySlug)
            .Select(p => new { p.Id, p.Slug, Name = p.Label.ToString() })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
        {
            return new PropertyDashboardAccessResult
            {
                PropertyNotFound = true
            };
        }

        return new PropertyDashboardAccessResult
        {
            IsAuthorized = true,
            Context = new PropertyDashboardContext
            {
                AppUserId = context.AppUserId,
                ManagementCompanyId = context.ManagementCompanyId,
                CompanySlug = context.CompanySlug,
                CompanyName = context.CompanyName,
                CustomerId = context.CustomerId,
                CustomerSlug = context.CustomerSlug,
                CustomerName = context.CustomerName,
                PropertyId = property.Id,
                PropertySlug = property.Slug,
                PropertyName = property.Name
            }
        };
    }

    public async Task<CompanyCustomerListResult> ListAsync(
        CustomerWorkspaceContext context,
        CancellationToken cancellationToken = default)
    {
        var customers = await _dbContext.Customers
            .AsNoTracking()
            .Where(c => c.ManagementCompanyId == context.ManagementCompanyId)
            .OrderBy(c => c.Name)
            .Select(c => new CompanyCustomerListItem
            {
                CustomerId = c.Id,
                CustomerSlug = c.Slug,
                Name = c.Name,
                RegistryCode = c.RegistryCode,
                BillingEmail = c.BillingEmail,
                BillingAddress = c.BillingAddress,
                Phone = c.Phone
            })
            .ToListAsync(cancellationToken);

        return new CompanyCustomerListResult
        {
            Customers = customers
        };
    }

    public async Task<CustomerCreateResult> CreateAsync(
        CustomerWorkspaceContext context,
        CustomerCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new CustomerCreateResult
            {
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.Name)
            };
        }

        if (string.IsNullOrWhiteSpace(request.RegistryCode))
        {
            return new CustomerCreateResult
            {
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.RegistryCode)
            };
        }

        var normalizedName = request.Name.Trim();
        var normalizedRegistryCode = request.RegistryCode.Trim();
        var normalizedBillingEmail = string.IsNullOrWhiteSpace(request.BillingEmail) ? null : request.BillingEmail.Trim();
        var normalizedBillingAddress = string.IsNullOrWhiteSpace(request.BillingAddress) ? null : request.BillingAddress.Trim();
        var normalizedPhone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedBillingEmail))
        {
            var emailAttribute = new EmailAddressAttribute();
            if (!emailAttribute.IsValid(normalizedBillingEmail))
            {
                return new CustomerCreateResult
                {
                    InvalidBillingEmail = true,
                    ErrorMessage = App.Resources.Views.UiText.InvalidEmailAddress
                };
            }
        }

        var duplicateRegistryCode = await _dbContext.Customers
            .AsNoTracking()
            .AnyAsync(c => c.ManagementCompanyId == context.ManagementCompanyId
                           && c.RegistryCode.ToLower() == normalizedRegistryCode.ToLower(), cancellationToken);

        if (duplicateRegistryCode)
        {
            return new CustomerCreateResult
            {
                DuplicateRegistryCode = true,
                ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("CustomerRegistryCodeAlreadyExists")
                               ?? "Customer with this registry code already exists in this company."
            };
        }

        var baseSlug = SlugGenerator.GenerateSlug(normalizedName);
        var existingSlugs = await _dbContext.Customers
            .AsNoTracking()
            .Where(c => c.ManagementCompanyId == context.ManagementCompanyId && c.Slug.StartsWith(baseSlug))
            .Select(c => c.Slug)
            .ToListAsync(cancellationToken);
        var uniqueSlug = SlugGenerator.EnsureUniqueSlug(baseSlug, existingSlugs);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            RegistryCode = normalizedRegistryCode,
            BillingEmail = normalizedBillingEmail,
            BillingAddress = normalizedBillingAddress,
            Phone = normalizedPhone,
            Slug = uniqueSlug,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ManagementCompanyId = context.ManagementCompanyId
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CustomerCreateResult
        {
            Success = true,
            CreatedCustomerId = customer.Id
        };
    }
}
