using App.BLL.Contracts.Onboarding.Commands;
using App.BLL.Contracts.Onboarding.Models;
using App.BLL.Contracts.Onboarding.Queries;
using App.BLL.Contracts.Onboarding.Services;
using App.Contracts;
using FluentResults;

namespace App.BLL.Onboarding.ContextSelection;

public class WorkspaceRedirectService : IWorkspaceRedirectService, IContextSelectionService
{
    private readonly IAppUOW _uow;
    private readonly IAccountOnboardingService _accountOnboardingService;
    private readonly IWorkspaceCatalogService _workspaceCatalogService;

    public WorkspaceRedirectService(
        IAppUOW uow,
        IAccountOnboardingService accountOnboardingService,
        IWorkspaceCatalogService workspaceCatalogService)
    {
        _uow = uow;
        _accountOnboardingService = accountOnboardingService;
        _workspaceCatalogService = workspaceCatalogService;
    }

    public async Task<Result<WorkspaceRedirectModel?>> ResolveContextRedirectAsync(
        ResolveWorkspaceRedirectQuery query,
        CancellationToken cancellationToken = default)
    {
        var cookieState = query.CookieState;

        if (cookieState.ContextType == "management" && !string.IsNullOrWhiteSpace(cookieState.ManagementCompanySlug))
        {
            var hasSelectedManagementAccess = await _accountOnboardingService.UserHasManagementCompanyAccessAsync(
                query.AppUserId,
                cookieState.ManagementCompanySlug,
                cancellationToken);
            if (hasSelectedManagementAccess)
            {
                return Result.Ok<WorkspaceRedirectModel?>(new WorkspaceRedirectModel
                {
                    Destination = WorkspaceRedirectDestination.ManagementDashboard,
                    CompanySlug = cookieState.ManagementCompanySlug
                });
            }
        }

        if (cookieState.ContextType == "resident")
        {
            var hasSelectedResidentContext = await _uow.Residents.HasActiveUserResidentContextAsync(
                query.AppUserId,
                cancellationToken);
            if (hasSelectedResidentContext)
            {
                return Result.Ok<WorkspaceRedirectModel?>(new WorkspaceRedirectModel
                {
                    Destination = WorkspaceRedirectDestination.ResidentDashboard
                });
            }
        }

        if (cookieState.ContextType == "customer" && Guid.TryParse(cookieState.CustomerId, out var selectedCustomerId))
        {
            var hasSelectedCustomerContext = await _uow.Customers.ActiveUserCustomerContextExistsAsync(
                query.AppUserId,
                selectedCustomerId,
                cancellationToken);
            if (hasSelectedCustomerContext)
            {
                return Result.Ok<WorkspaceRedirectModel?>(new WorkspaceRedirectModel
                {
                    Destination = WorkspaceRedirectDestination.CustomerDashboard
                });
            }
        }

        var defaultManagementCompanySlug = await _accountOnboardingService.GetDefaultManagementCompanySlugAsync(
            query.AppUserId,
            cancellationToken);
        if (!string.IsNullOrWhiteSpace(defaultManagementCompanySlug))
        {
            return Result.Ok<WorkspaceRedirectModel?>(new WorkspaceRedirectModel
            {
                Destination = WorkspaceRedirectDestination.ManagementDashboard,
                CompanySlug = defaultManagementCompanySlug
            });
        }

        var hasResidentContext = await _uow.Residents.HasActiveUserResidentContextAsync(
            query.AppUserId,
            cancellationToken);
        if (hasResidentContext)
        {
            return Result.Ok<WorkspaceRedirectModel?>(new WorkspaceRedirectModel
            {
                Destination = WorkspaceRedirectDestination.ResidentDashboard
            });
        }

        var hasCustomerContext = (await _uow.Customers.ActiveUserCustomerContextsAsync(
            query.AppUserId,
            cancellationToken)).Count > 0;
        if (hasCustomerContext)
        {
            return Result.Ok<WorkspaceRedirectModel?>(new WorkspaceRedirectModel
            {
                Destination = WorkspaceRedirectDestination.CustomerDashboard
            });
        }

        return Result.Ok<WorkspaceRedirectModel?>(null);
    }

    public async Task<Result<WorkspaceSelectionAuthorizationModel>> AuthorizeContextSelectionAsync(
        AuthorizeContextSelectionQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedType = query.ContextType.Trim().ToLowerInvariant();

        switch (normalizedType)
        {
            case "management":
                if (!query.ContextId.HasValue)
                {
                    return Result.Ok(new WorkspaceSelectionAuthorizationModel
                    {
                        Authorized = false,
                        NormalizedType = normalizedType
                    });
                }

                var managementCompany = await _uow.ManagementCompanies.ActiveUserManagementContextByCompanyIdAsync(
                    query.AppUserId,
                    query.ContextId.Value,
                    cancellationToken);

                if (managementCompany == null)
                {
                    return Result.Ok(new WorkspaceSelectionAuthorizationModel
                    {
                        Authorized = false,
                        NormalizedType = normalizedType
                    });
                }

                return Result.Ok(new WorkspaceSelectionAuthorizationModel
                {
                    Authorized = true,
                    NormalizedType = normalizedType,
                    ManagementCompanyId = managementCompany.ManagementCompanyId,
                    ManagementCompanySlug = managementCompany.Slug
                });

            case "customer":
                if (!query.ContextId.HasValue)
                {
                    return Result.Ok(new WorkspaceSelectionAuthorizationModel
                    {
                        Authorized = false,
                        NormalizedType = normalizedType
                    });
                }

                var hasCustomerContext = await _uow.Customers.ActiveUserCustomerContextExistsAsync(
                    query.AppUserId,
                    query.ContextId.Value,
                    cancellationToken);

                return Result.Ok(new WorkspaceSelectionAuthorizationModel
                {
                    Authorized = hasCustomerContext,
                    NormalizedType = normalizedType,
                    CustomerId = hasCustomerContext ? query.ContextId : null
                });

            case "resident":
                var hasResidentContext = await _uow.Residents.HasActiveUserResidentContextAsync(
                    query.AppUserId,
                    cancellationToken);

                return Result.Ok(new WorkspaceSelectionAuthorizationModel
                {
                    Authorized = hasResidentContext,
                    NormalizedType = normalizedType
                });

            default:
                return Result.Ok(new WorkspaceSelectionAuthorizationModel
                {
                    Authorized = false,
                    NormalizedType = normalizedType
                });
        }
    }

    public Task<Result<WorkspaceCatalogModel>> GetWorkspaceCatalogAsync(
        GetWorkspaceCatalogQuery query,
        CancellationToken cancellationToken = default)
    {
        return _workspaceCatalogService.GetWorkspaceCatalogAsync(query, cancellationToken);
    }

    public async Task<Result> SelectWorkspaceAsync(
        SelectWorkspaceCommand command,
        CancellationToken cancellationToken = default)
    {
        var authorization = await AuthorizeContextSelectionAsync(
            new AuthorizeContextSelectionQuery
            {
                AppUserId = command.AppUserId,
                ContextType = command.ContextType,
                ContextId = command.ContextId
            },
            cancellationToken);

        return authorization.IsSuccess && authorization.Value.Authorized
            ? Result.Ok()
            : Result.Fail("Workspace context is not available.");
    }
}
