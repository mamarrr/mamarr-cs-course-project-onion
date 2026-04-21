using App.BLL.Onboarding.Account;
using App.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Onboarding.Api;

public interface IApiOnboardingContextService
{
    Task<ApiOnboardingContextCatalogResult> GetContextsAsync(Guid appUserId, CancellationToken cancellationToken = default);
}

public sealed class ApiOnboardingContextCatalogResult
{
    public IReadOnlyList<ApiOnboardingContextEntry> Contexts { get; init; } = Array.Empty<ApiOnboardingContextEntry>();
    public ApiOnboardingContextEntry? DefaultContext { get; init; }
}

public sealed class ApiOnboardingContextEntry
{
    public string ContextType { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
    public string? CompanySlug { get; init; }
    public string? CompanyName { get; init; }
    public Guid? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string? ResidentDisplayName { get; init; }
}

public sealed class ApiOnboardingContextService : IApiOnboardingContextService
{
    private readonly AppDbContext _dbContext;
    private readonly IOnboardingService _onboardingService;

    public ApiOnboardingContextService(AppDbContext dbContext, IOnboardingService onboardingService)
    {
        _dbContext = dbContext;
        _onboardingService = onboardingService;
    }

    public async Task<ApiOnboardingContextCatalogResult> GetContextsAsync(Guid appUserId, CancellationToken cancellationToken = default)
    {
        var contexts = new List<ApiOnboardingContextEntry>();
        var defaultManagementCompanySlug = await _onboardingService.GetDefaultManagementCompanySlugAsync(appUserId);

        var managementContexts = await _dbContext.ManagementCompanyUsers
            .Where(x => x.AppUserId == appUserId
                        && x.IsActive
                        && x.ManagementCompany != null
                        && x.ManagementCompany.IsActive)
            .Select(x => new
            {
                x.ManagementCompanyId,
                x.ManagementCompany!.Slug,
                x.ManagementCompany.Name
            })
            .Distinct()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        contexts.AddRange(managementContexts.Select(company => new ApiOnboardingContextEntry
        {
            ContextType = "management",
            Label = company.Name,
            IsDefault = string.Equals(company.Slug, defaultManagementCompanySlug, StringComparison.OrdinalIgnoreCase),
            CompanySlug = company.Slug,
            CompanyName = company.Name
        }));

        var customerContexts = await (
                from residentUser in _dbContext.ResidentUsers
                join customerRepresentative in _dbContext.CustomerRepresentatives
                    on residentUser.ResidentId equals customerRepresentative.ResidentId
                join customer in _dbContext.Customers
                    on customerRepresentative.CustomerId equals customer.Id
                where residentUser.AppUserId == appUserId
                      && residentUser.IsActive
                      && customerRepresentative.IsActive
                      && customer.IsActive
                select new
                {
                    customer.Id,
                    customer.Name
                })
            .Distinct()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        contexts.AddRange(customerContexts.Select(customer => new ApiOnboardingContextEntry
        {
            ContextType = "customer",
            Label = customer.Name,
            CustomerId = customer.Id,
            CustomerName = customer.Name
        }));

        var residentContext = await (
                from residentUser in _dbContext.ResidentUsers
                join resident in _dbContext.Residents on residentUser.ResidentId equals resident.Id
                where residentUser.AppUserId == appUserId
                      && residentUser.IsActive
                      && resident.IsActive
                select new
                {
                    resident.FirstName,
                    resident.LastName,
                    resident.IdCode
                })
            .FirstOrDefaultAsync(cancellationToken);

        if (residentContext != null)
        {
            var residentDisplayName = string.Join(" ", new[] { residentContext.FirstName, residentContext.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
            if (string.IsNullOrWhiteSpace(residentDisplayName))
            {
                residentDisplayName = residentContext.IdCode;
            }

            contexts.Add(new ApiOnboardingContextEntry
            {
                ContextType = "resident",
                Label = residentDisplayName,
                ResidentDisplayName = residentDisplayName
            });
        }

        var defaultContext = contexts.FirstOrDefault(x => x.IsDefault)
                             ?? contexts.FirstOrDefault(x => x.ContextType == "resident")
                             ?? contexts.FirstOrDefault(x => x.ContextType == "customer");

        if (defaultContext != null)
        {
            contexts = contexts
                .Select(x => new ApiOnboardingContextEntry
                {
                    ContextType = x.ContextType,
                    Label = x.Label,
                    IsDefault = AreSameContext(x, defaultContext),
                    CompanySlug = x.CompanySlug,
                    CompanyName = x.CompanyName,
                    CustomerId = x.CustomerId,
                    CustomerName = x.CustomerName,
                    ResidentDisplayName = x.ResidentDisplayName
                })
                .ToList();
            defaultContext = contexts.First(x => x.IsDefault);
        }

        return new ApiOnboardingContextCatalogResult
        {
            Contexts = contexts,
            DefaultContext = defaultContext
        };
    }

    private static bool AreSameContext(ApiOnboardingContextEntry left, ApiOnboardingContextEntry right)
    {
        if (!string.Equals(left.ContextType, right.ContextType, StringComparison.Ordinal)) return false;

        return left.ContextType switch
        {
            "management" => string.Equals(left.CompanySlug, right.CompanySlug, StringComparison.OrdinalIgnoreCase),
            "customer" => left.CustomerId == right.CustomerId,
            "resident" => true,
            _ => false
        };
    }
}
