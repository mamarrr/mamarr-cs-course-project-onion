using App.BLL.Onboarding.Account;
using App.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Onboarding.ContextSelection;

public class WorkspaceRedirectService : IWorkspaceRedirectService
{
    private readonly AppDbContext _dbContext;
    private readonly IAccountOnboardingService _accountOnboardingService;

    public WorkspaceRedirectService(AppDbContext dbContext, IAccountOnboardingService accountOnboardingService)
    {
        _dbContext = dbContext;
        _accountOnboardingService = accountOnboardingService;
    }

    public async Task<WorkspaceRedirectTarget?> ResolveContextRedirectAsync(
        Guid appUserId,
        WorkspaceRedirectCookieState cookieState,
        CancellationToken cancellationToken = default)
    {
        if (cookieState.ContextType == "management" && !string.IsNullOrWhiteSpace(cookieState.ManagementCompanySlug))
        {
            var hasSelectedManagementAccess = await _accountOnboardingService.UserHasManagementCompanyAccessAsync(
                appUserId,
                cookieState.ManagementCompanySlug);
            if (hasSelectedManagementAccess)
            {
                return new WorkspaceRedirectTarget
                {
                    Destination = WorkspaceRedirectDestination.ManagementDashboard,
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
                return new WorkspaceRedirectTarget
                {
                    Destination = WorkspaceRedirectDestination.ResidentDashboard
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
                return new WorkspaceRedirectTarget
                {
                    Destination = WorkspaceRedirectDestination.CustomerDashboard
                };
            }
        }

        var defaultManagementCompanySlug = await _accountOnboardingService.GetDefaultManagementCompanySlugAsync(appUserId);
        if (!string.IsNullOrWhiteSpace(defaultManagementCompanySlug))
        {
            return new WorkspaceRedirectTarget
            {
                Destination = WorkspaceRedirectDestination.ManagementDashboard,
                CompanySlug = defaultManagementCompanySlug
            };
        }

        var hasResidentContext = await _dbContext.ResidentUsers
            .AnyAsync(x => x.AppUserId == appUserId && x.IsActive, cancellationToken);
        if (hasResidentContext)
        {
            return new WorkspaceRedirectTarget
            {
                Destination = WorkspaceRedirectDestination.ResidentDashboard
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
            return new WorkspaceRedirectTarget
            {
                Destination = WorkspaceRedirectDestination.CustomerDashboard
            };
        }

        return null;
    }

    public async Task<WorkspaceRedirectAuthorizationResult> AuthorizeContextSelectionAsync(
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
                    return new WorkspaceRedirectAuthorizationResult { Authorized = false, NormalizedType = normalizedType };
                }

                var managementCompany = await _dbContext.ManagementCompanyUsers
                    .Where(x => x.AppUserId == appUserId && x.IsActive && x.ManagementCompanyId == contextId.Value)
                    .Select(x => new { x.ManagementCompanyId, x.ManagementCompany!.Slug })
                    .FirstOrDefaultAsync(cancellationToken);

                if (managementCompany == null)
                {
                    return new WorkspaceRedirectAuthorizationResult { Authorized = false, NormalizedType = normalizedType };
                }

                return new WorkspaceRedirectAuthorizationResult
                {
                    Authorized = true,
                    NormalizedType = normalizedType,
                    ManagementCompanyId = managementCompany.ManagementCompanyId,
                    ManagementCompanySlug = managementCompany.Slug
                };

            case "customer":
                if (!contextId.HasValue)
                {
                    return new WorkspaceRedirectAuthorizationResult { Authorized = false, NormalizedType = normalizedType };
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

                return new WorkspaceRedirectAuthorizationResult
                {
                    Authorized = hasCustomerContext,
                    NormalizedType = normalizedType,
                    CustomerId = hasCustomerContext ? contextId : null
                };

            case "resident":
                var hasResidentContext = await _dbContext.ResidentUsers
                    .AnyAsync(x => x.AppUserId == appUserId && x.IsActive, cancellationToken);

                return new WorkspaceRedirectAuthorizationResult
                {
                    Authorized = hasResidentContext,
                    NormalizedType = normalizedType
                };

            default:
                return new WorkspaceRedirectAuthorizationResult
                {
                    Authorized = false,
                    NormalizedType = normalizedType
                };
        }
    }
}
