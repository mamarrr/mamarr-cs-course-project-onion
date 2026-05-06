using App.BLL.Contracts.Onboarding;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Onboarding.Commands;
using App.BLL.DTO.Onboarding.Models;
using App.BLL.DTO.Onboarding.Queries;
using App.DAL.Contracts;
using FluentResults;

namespace App.BLL.Services.Onboarding;

public class WorkspaceService : IWorkspaceService
{
    private readonly IAppUOW _uow;
    private readonly IOnboardingService _onboardingService;

    public WorkspaceService(
        IAppUOW uow,
        IOnboardingService onboardingService)
    {
        _uow = uow;
        _onboardingService = onboardingService;
    }

    public async Task<Result<WorkspaceContextCatalogModel>> GetContextsAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        var contexts = new List<WorkspaceContextModel>();
        var defaultManagementCompanySlug = (await _onboardingService.GetDefaultManagementCompanySlugAsync(
            appUserId,
            cancellationToken)).Value;

        var managementContexts = await _uow.ManagementCompanies.ActiveUserManagementContextsAsync(
            appUserId,
            cancellationToken);
        contexts.AddRange(managementContexts.Select(company => new WorkspaceContextModel
        {
            ContextType = "management",
            Label = company.CompanyName,
            IsDefault = string.Equals(company.Slug, defaultManagementCompanySlug, StringComparison.OrdinalIgnoreCase),
            CompanySlug = company.Slug,
            CompanyName = company.CompanyName
        }));

        var customerContexts = await _uow.Customers.ActiveUserCustomerContextsAsync(
            appUserId,
            cancellationToken);
        contexts.AddRange(customerContexts.Select(customer => new WorkspaceContextModel
        {
            ContextType = "customer",
            Label = customer.Name,
            CustomerId = customer.CustomerId,
            CustomerName = customer.Name
        }));

        var residentContext = await _uow.Residents.FirstActiveUserResidentContextAsync(
            appUserId,
            cancellationToken);
        if (residentContext != null)
        {
            contexts.Add(new WorkspaceContextModel
            {
                ContextType = "resident",
                Label = residentContext.DisplayName,
                ResidentDisplayName = residentContext.DisplayName
            });
        }

        var defaultContext = contexts.FirstOrDefault(context => context.IsDefault)
                             ?? contexts.FirstOrDefault(context => context.ContextType == "resident")
                             ?? contexts.FirstOrDefault(context => context.ContextType == "customer");

        if (defaultContext != null)
        {
            contexts = contexts
                .Select(context => new WorkspaceContextModel
                {
                    ContextType = context.ContextType,
                    Label = context.Label,
                    IsDefault = AreSameContext(context, defaultContext),
                    CompanySlug = context.CompanySlug,
                    CompanyName = context.CompanyName,
                    CustomerId = context.CustomerId,
                    CustomerName = context.CustomerName,
                    ResidentDisplayName = context.ResidentDisplayName
                })
                .ToList();
            defaultContext = contexts.First(context => context.IsDefault);
        }

        return Result.Ok(new WorkspaceContextCatalogModel
        {
            Contexts = contexts,
            DefaultContext = defaultContext
        });
    }

    public Task<Result<WorkspaceCatalogModel>> GetCatalogAsync(
        App.BLL.DTO.Common.Routes.ManagementCompanyRoute route,
        CancellationToken cancellationToken = default)
    {
        return GetWorkspaceCatalogAsync(
            new GetWorkspaceCatalogQuery
            {
                AppUserId = route.AppUserId,
                CompanySlug = route.CompanySlug
            },
            cancellationToken);
    }

    public async Task<Result<WorkspaceRedirectModel?>> ResolveContextRedirectAsync(
        ResolveWorkspaceRedirectQuery query,
        CancellationToken cancellationToken = default)
    {
        var rememberedContext = query.RememberedContext;

        if (rememberedContext.ContextType == "management" && !string.IsNullOrWhiteSpace(rememberedContext.ManagementCompanySlug))
        {
            var hasSelectedManagementAccess = await _onboardingService.UserHasManagementCompanyAccessAsync(
                new App.BLL.DTO.Common.Routes.ManagementCompanyRoute
                {
                    AppUserId = query.AppUserId,
                    CompanySlug = rememberedContext.ManagementCompanySlug
                },
                cancellationToken);
            if (hasSelectedManagementAccess.Value)
            {
                return Result.Ok<WorkspaceRedirectModel?>(new WorkspaceRedirectModel
                {
                    Destination = WorkspaceRedirectDestination.ManagementDashboard,
                    CompanySlug = rememberedContext.ManagementCompanySlug
                });
            }
        }

        if (rememberedContext.ContextType == "resident")
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

        if (rememberedContext.ContextType == "customer" && Guid.TryParse(rememberedContext.CustomerId, out var selectedCustomerId))
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

        var defaultManagementCompanySlug = await _onboardingService.GetDefaultManagementCompanySlugAsync(
            query.AppUserId,
            cancellationToken);
        if (!string.IsNullOrWhiteSpace(defaultManagementCompanySlug.Value))
        {
            return Result.Ok<WorkspaceRedirectModel?>(new WorkspaceRedirectModel
            {
                Destination = WorkspaceRedirectDestination.ManagementDashboard,
                CompanySlug = defaultManagementCompanySlug.Value
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

    private async Task<Result<WorkspaceCatalogModel>> GetWorkspaceCatalogAsync(
        GetWorkspaceCatalogQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = query.CompanySlug.Trim();

        var managementContexts = await _uow.ManagementCompanies.ActiveUserManagementContextsAsync(
            query.AppUserId,
            cancellationToken);

        var managementOptions = managementContexts
            .Select(context => new WorkspaceOptionModel
            {
                Id = context.ManagementCompanyId,
                ContextType = "management",
                Name = context.CompanyName,
                Slug = context.Slug,
                ManagementCompanySlug = context.Slug,
                IsDefault = string.Equals(context.Slug, managementContexts.FirstOrDefault()?.Slug, StringComparison.OrdinalIgnoreCase)
            })
            .ToList();

        var selectedManagementContext = managementContexts
            .FirstOrDefault(context => string.Equals(context.Slug, normalizedSlug, StringComparison.OrdinalIgnoreCase));

        var managementCompanyName = selectedManagementContext?.CompanyName
                                    ?? managementContexts.Select(context => context.CompanyName).FirstOrDefault()
                                    ?? "Management Workspace";

        var canManageCompanyUsers = selectedManagementContext is not null
                                    && CompanyUserManagerRoles.Contains(selectedManagementContext.RoleCode);

        var customerOptions = (await _uow.Customers.ActiveUserCustomerContextsAsync(
                query.AppUserId,
                cancellationToken))
            .Select(customer => new WorkspaceOptionModel
            {
                Id = customer.CustomerId,
                ContextType = "customer",
                Name = customer.Name,
                Slug = customer.Slug,
                ManagementCompanySlug = customer.ManagementCompanySlug
            })
            .ToList();

        var residentContext = await _uow.Residents.FirstActiveUserResidentContextAsync(
            query.AppUserId,
            cancellationToken);

        var residentOption = residentContext is null
            ? null
            : new WorkspaceOptionModel
            {
                Id = residentContext.ResidentId,
                ContextType = "resident",
                Name = residentContext.DisplayName,
                Slug = residentContext.IdCode,
                ManagementCompanySlug = residentContext.ManagementCompanySlug
            };

        var defaultContext = managementOptions.FirstOrDefault(option => option.IsDefault)
                             ?? residentOption
                             ?? customerOptions.FirstOrDefault();

        return Result.Ok(new WorkspaceCatalogModel
        {
            ManagementCompanyName = managementCompanyName,
            CanManageCompanyUsers = canManageCompanyUsers,
            HasResidentContext = residentOption is not null,
            ManagementCompanies = managementOptions,
            Customers = customerOptions,
            Resident = residentOption,
            DefaultContext = defaultContext
        });
    }

    public async Task<Result> SelectAsync(
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
            : Result.Fail(new ForbiddenError("Workspace context is not available."));
    }

    private static readonly HashSet<string> CompanyUserManagerRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER"
    };

    private static bool AreSameContext(WorkspaceContextModel left, WorkspaceContextModel right)
    {
        if (!string.Equals(left.ContextType, right.ContextType, StringComparison.Ordinal))
        {
            return false;
        }

        return left.ContextType switch
        {
            "management" => string.Equals(left.CompanySlug, right.CompanySlug, StringComparison.OrdinalIgnoreCase),
            "customer" => left.CustomerId == right.CustomerId,
            "resident" => true,
            _ => false
        };
    }
}
