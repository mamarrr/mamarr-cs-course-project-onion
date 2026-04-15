using System.ComponentModel.DataAnnotations;
using App.BLL.Routing;
using App.DAL.EF;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.ManagementCustomers;

public class ManagementCustomersService : IManagementCustomersService
{
    private static readonly HashSet<string> AllowedRoleCodes =
    [
        "OWNER",
        "MANAGER",
        "FINANCE",
        "SUPPORT"
    ];

    private readonly AppDbContext _dbContext;

    public ManagementCustomersService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ManagementCustomersAuthorizationResult> AuthorizeAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = companySlug.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return new ManagementCustomersAuthorizationResult
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
            return new ManagementCustomersAuthorizationResult
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
            return new ManagementCustomersAuthorizationResult
            {
                IsForbidden = true,
                ErrorMessage = App.Resources.Views.UiText.AccessDeniedDescription
            };
        }

        return new ManagementCustomersAuthorizationResult
        {
            IsAuthorized = true,
            Context = new ManagementCustomersAuthorizedContext
            {
                AppUserId = appUserId,
                ManagementCompanyId = company.Id,
                CompanySlug = company.Slug,
                CompanyName = company.Name
            }
        };
    }

    public async Task<ManagementCustomerListResult> ListAsync(
        ManagementCustomersAuthorizedContext context,
        CancellationToken cancellationToken = default)
    {
        var customers = await _dbContext.Customers
            .AsNoTracking()
            .Where(c => c.ManagementCompanyId == context.ManagementCompanyId)
            .OrderBy(c => c.Name)
            .Select(c => new ManagementCustomerListItem
            {
                CustomerId = c.Id,
                Name = c.Name,
                RegistryCode = c.RegistryCode,
                BillingEmail = c.BillingEmail,
                BillingAddress = c.BillingAddress,
                Phone = c.Phone
            })
            .ToListAsync(cancellationToken);

        return new ManagementCustomerListResult
        {
            Customers = customers
        };
    }

    public async Task<ManagementCustomerCreateResult> CreateAsync(
        ManagementCustomersAuthorizedContext context,
        ManagementCustomerCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new ManagementCustomerCreateResult
            {
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.Name)
            };
        }

        if (string.IsNullOrWhiteSpace(request.RegistryCode))
        {
            return new ManagementCustomerCreateResult
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
                return new ManagementCustomerCreateResult
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
            return new ManagementCustomerCreateResult
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

        return new ManagementCustomerCreateResult
        {
            Success = true,
            CreatedCustomerId = customer.Id
        };
    }
}
