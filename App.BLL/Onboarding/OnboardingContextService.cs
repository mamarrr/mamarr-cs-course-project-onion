using App.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Onboarding;

public class OnboardingContextService : IOnboardingContextService
{
    private readonly AppDbContext _dbContext;
    private readonly IOnboardingService _onboardingService;

    public OnboardingContextService(AppDbContext dbContext, IOnboardingService onboardingService)
    {
        _dbContext = dbContext;
        _onboardingService = onboardingService;
    }

    public async Task<OnboardingContextRedirectTarget?> ResolveContextRedirectAsync(
        Guid appUserId,
        OnboardingContextSelectionCookieState cookieState,
        CancellationToken cancellationToken = default)
    {
        if (cookieState.ContextType == "management" && !string.IsNullOrWhiteSpace(cookieState.ManagementCompanySlug))
        {
            var hasSelectedManagementAccess = await _onboardingService.UserHasManagementCompanyAccessAsync(
                appUserId,
                cookieState.ManagementCompanySlug);
            if (hasSelectedManagementAccess)
            {
                return new OnboardingContextRedirectTarget
                {
                    Destination = OnboardingContextRedirectDestination.ManagementDashboard,
                    CompanySlug = cookieState.ManagementCompanySlug
                };
            }
        }

        if (cookieState.ContextType == "resident")
        {
            var hasSelectedResidentContext = await _dbContext.ResidentUsers
                .AnyAsync(x => x.AppUserId == appUserId && x.IsActive, cancellationToken);
            if (hasSelectedResidentContext)
            {
                return new OnboardingContextRedirectTarget
                {
                    Destination = OnboardingContextRedirectDestination.ResidentDashboard
                };
            }
        }

        if (cookieState.ContextType == "customer" && Guid.TryParse(cookieState.CustomerId, out var selectedCustomerId))
        {
            var hasSelectedCustomerContext = await (
                    from residentUser in _dbContext.ResidentUsers
                    join customerRepresentative in _dbContext.CustomerRepresentatives
                        on residentUser.ResidentId equals customerRepresentative.ResidentId
                    where residentUser.AppUserId == appUserId
                          && residentUser.IsActive
                          && customerRepresentative.IsActive
                          && customerRepresentative.CustomerId == selectedCustomerId
                    select customerRepresentative.Id)
                .AnyAsync(cancellationToken);
            if (hasSelectedCustomerContext)
            {
                return new OnboardingContextRedirectTarget
                {
                    Destination = OnboardingContextRedirectDestination.CustomerDashboard
                };
            }
        }

        var defaultManagementCompanySlug = await _onboardingService.GetDefaultManagementCompanySlugAsync(appUserId);
        if (!string.IsNullOrWhiteSpace(defaultManagementCompanySlug))
        {
            return new OnboardingContextRedirectTarget
            {
                Destination = OnboardingContextRedirectDestination.ManagementDashboard,
                CompanySlug = defaultManagementCompanySlug
            };
        }

        var hasResidentContext = await _dbContext.ResidentUsers
            .AnyAsync(x => x.AppUserId == appUserId && x.IsActive, cancellationToken);
        if (hasResidentContext)
        {
            return new OnboardingContextRedirectTarget
            {
                Destination = OnboardingContextRedirectDestination.ResidentDashboard
            };
        }

        var hasCustomerContext = await (
                from residentUser in _dbContext.ResidentUsers
                join customerRepresentative in _dbContext.CustomerRepresentatives
                    on residentUser.ResidentId equals customerRepresentative.ResidentId
                where residentUser.AppUserId == appUserId
                      && residentUser.IsActive
                      && customerRepresentative.IsActive
                select customerRepresentative.Id)
            .AnyAsync(cancellationToken);
        if (hasCustomerContext)
        {
            return new OnboardingContextRedirectTarget
            {
                Destination = OnboardingContextRedirectDestination.CustomerDashboard
            };
        }

        return null;
    }

    public async Task<OnboardingContextSelectionAuthorizationResult> AuthorizeContextSelectionAsync(
        Guid appUserId,
        string contextType,
        Guid? contextId,
        CancellationToken cancellationToken = default)
    {
        var normalizedType = contextType.Trim().ToLowerInvariant();

        switch (normalizedType)
        {
            case "management":
                if (!contextId.HasValue)
                {
                    return new OnboardingContextSelectionAuthorizationResult { Authorized = false, NormalizedType = normalizedType };
                }

                var managementCompany = await _dbContext.ManagementCompanyUsers
                    .Where(x => x.AppUserId == appUserId && x.IsActive && x.ManagementCompanyId == contextId.Value)
                    .Select(x => new { x.ManagementCompanyId, x.ManagementCompany!.Slug })
                    .FirstOrDefaultAsync(cancellationToken);

                if (managementCompany == null)
                {
                    return new OnboardingContextSelectionAuthorizationResult { Authorized = false, NormalizedType = normalizedType };
                }

                return new OnboardingContextSelectionAuthorizationResult
                {
                    Authorized = true,
                    NormalizedType = normalizedType,
                    ManagementCompanyId = managementCompany.ManagementCompanyId,
                    ManagementCompanySlug = managementCompany.Slug
                };

            case "customer":
                if (!contextId.HasValue)
                {
                    return new OnboardingContextSelectionAuthorizationResult { Authorized = false, NormalizedType = normalizedType };
                }

                var hasCustomerContext = await (
                        from residentUser in _dbContext.ResidentUsers
                        join customerRepresentative in _dbContext.CustomerRepresentatives
                            on residentUser.ResidentId equals customerRepresentative.ResidentId
                        where residentUser.AppUserId == appUserId
                              && residentUser.IsActive
                              && customerRepresentative.IsActive
                              && customerRepresentative.CustomerId == contextId.Value
                        select customerRepresentative.Id)
                    .AnyAsync(cancellationToken);

                return new OnboardingContextSelectionAuthorizationResult
                {
                    Authorized = hasCustomerContext,
                    NormalizedType = normalizedType,
                    CustomerId = hasCustomerContext ? contextId : null
                };

            case "resident":
                var hasResidentContext = await _dbContext.ResidentUsers
                    .AnyAsync(x => x.AppUserId == appUserId && x.IsActive, cancellationToken);

                return new OnboardingContextSelectionAuthorizationResult
                {
                    Authorized = hasResidentContext,
                    NormalizedType = normalizedType
                };

            default:
                return new OnboardingContextSelectionAuthorizationResult
                {
                    Authorized = false,
                    NormalizedType = normalizedType
                };
        }
    }
}
