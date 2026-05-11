using App.BLL.Contracts.Workspace;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Workspace.Models;
using App.BLL.DTO.Workspace.Queries;
using App.DAL.Contracts;
using FluentResults;

namespace App.BLL.Services.Workspace;

public class WorkspaceService : IWorkspaceService
{
    private readonly IAppUOW _uow;

    public WorkspaceService(
        IAppUOW uow)
    {
        _uow = uow;
    }

    public async Task<Result<bool>> HasAnyContextAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        var hasManagementContext = (await _uow.ManagementCompanies.ActiveUserManagementContextsAsync(
            appUserId,
            cancellationToken)).Count > 0;

        if (hasManagementContext)
        {
            return Result.Ok(true);
        }

        return Result.Ok(await _uow.Residents.HasActiveUserResidentContextAsync(appUserId, cancellationToken));
    }

    public async Task<Result<string?>> GetDefaultManagementCompanySlugAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        return Result.Ok<string?>((await _uow.ManagementCompanies.ActiveUserManagementContextsAsync(
                appUserId,
                cancellationToken))
            .Select(context => context.Slug)
            .FirstOrDefault());
    }

    public Task<Result<bool>> UserHasManagementCompanyAccessAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(route.CompanySlug))
        {
            return Task.FromResult(Result.Ok(false));
        }

        return HasManagementCompanyAccessAsync(route, cancellationToken);
    }

    public async Task<Result<WorkspaceCatalogModel>> GetCatalogAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = route.CompanySlug.Trim();

        var managementContexts = await _uow.ManagementCompanies.ActiveUserManagementContextsAsync(
            route.AppUserId,
            cancellationToken);

        var managementOptions = managementContexts
            .Select(context => new WorkspaceOptionModel
            {
                Id = context.ManagementCompanyId,
                ContextType = "management",
                Name = context.CompanyName,
                Slug = context.Slug,
                ManagementCompanySlug = context.Slug,
                IsDefault = string.Equals(context.Slug, managementContexts.FirstOrDefault()?.Slug, StringComparison.OrdinalIgnoreCase),
                CanManageCompanyUsers = CompanyUserManagerRoles.Contains(context.RoleCode)
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
                route.AppUserId,
                cancellationToken))
            .Select(customer => new WorkspaceOptionModel
            {
                Id = customer.CustomerId,
                ContextType = "customer",
                Name = customer.Name,
                Slug = customer.Slug,
                ManagementCompanySlug = customer.ManagementCompanySlug,
                CanManageCompanyUsers = false
            })
            .ToList();

        var residentContext = await _uow.Residents.FirstActiveUserResidentContextAsync(
            route.AppUserId,
            cancellationToken);

        var residentOption = residentContext is null
            ? null
            : new WorkspaceOptionModel
            {
                Id = residentContext.ResidentId,
                ContextType = "resident",
                Name = residentContext.DisplayName,
                Slug = residentContext.IdCode,
                ManagementCompanySlug = residentContext.ManagementCompanySlug,
                CanManageCompanyUsers = false
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

    public async Task<Result<UserWorkspaceCatalogModel>> GetUserCatalogAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        var managementContexts = await _uow.ManagementCompanies.ActiveUserManagementContextsAsync(
            appUserId,
            cancellationToken);

        var managementOptions = managementContexts
            .Select(context => new WorkspaceOptionModel
            {
                Id = context.ManagementCompanyId,
                ContextType = "management",
                Name = context.CompanyName,
                Slug = context.Slug,
                ManagementCompanySlug = context.Slug,
                IsDefault = string.Equals(context.Slug, managementContexts.FirstOrDefault()?.Slug, StringComparison.OrdinalIgnoreCase),
                CanManageCompanyUsers = CompanyUserManagerRoles.Contains(context.RoleCode)
            })
            .ToList();

        var customerOptions = (await _uow.Customers.ActiveUserCustomerContextsAsync(
                appUserId,
                cancellationToken))
            .Select(customer => new WorkspaceOptionModel
            {
                Id = customer.CustomerId,
                ContextType = "customer",
                Name = customer.Name,
                Slug = customer.Slug,
                ManagementCompanySlug = customer.ManagementCompanySlug,
                CanManageCompanyUsers = false
            })
            .ToList();

        var residentContext = await _uow.Residents.FirstActiveUserResidentContextAsync(
            appUserId,
            cancellationToken);

        IReadOnlyList<WorkspaceOptionModel> residentOptions = residentContext is null
            ? []
            : new List<WorkspaceOptionModel>
            {
                new()
                {
                    Id = residentContext.ResidentId,
                    ContextType = "resident",
                    Name = residentContext.DisplayName,
                    Slug = residentContext.IdCode,
                    ManagementCompanySlug = residentContext.ManagementCompanySlug,
                    CanManageCompanyUsers = false
                }
            };

        var defaultContext = managementOptions.FirstOrDefault()
                             ?? residentOptions.FirstOrDefault()
                             ?? customerOptions.FirstOrDefault();

        return Result.Ok(new UserWorkspaceCatalogModel
        {
            ManagementCompanies = managementOptions,
            Customers = customerOptions,
            Residents = residentOptions,
            DefaultContext = defaultContext
        });
    }

    public async Task<Result<WorkspaceEntryPointModel?>> ResolveWorkspaceEntryPointAsync(
        ResolveWorkspaceEntryPointQuery query,
        CancellationToken cancellationToken = default)
    {
        var rememberedContext = query.RememberedContext;

        if (rememberedContext.ContextType == "management" && !string.IsNullOrWhiteSpace(rememberedContext.ManagementCompanySlug))
        {
            var hasSelectedManagementAccess = await UserHasManagementCompanyAccessAsync(
                new ManagementCompanyRoute
                {
                    AppUserId = query.AppUserId,
                    CompanySlug = rememberedContext.ManagementCompanySlug
                },
                cancellationToken);
            if (hasSelectedManagementAccess.Value)
            {
                return Result.Ok<WorkspaceEntryPointModel?>(new WorkspaceEntryPointModel
                {
                    Kind = WorkspaceEntryPointKind.ManagementDashboard,
                    CompanySlug = rememberedContext.ManagementCompanySlug
                });
            }
        }

        if (rememberedContext.ContextType == "resident")
        {
            var residentContext = await _uow.Residents.FirstActiveUserResidentContextAsync(
                query.AppUserId,
                cancellationToken);
            if (residentContext is not null)
            {
                return Result.Ok<WorkspaceEntryPointModel?>(new WorkspaceEntryPointModel
                {
                    Kind = WorkspaceEntryPointKind.ResidentDashboard,
                    ContextId = residentContext.ResidentId,
                    CompanySlug = residentContext.ManagementCompanySlug,
                    ResidentIdCode = residentContext.IdCode
                });
            }
        }

        if (rememberedContext.ContextType == "customer" && Guid.TryParse(rememberedContext.CustomerId, out var selectedCustomerId))
        {
            var customerContext = (await _uow.Customers.ActiveUserCustomerContextsAsync(
                    query.AppUserId,
                    cancellationToken))
                .FirstOrDefault(customer => customer.CustomerId == selectedCustomerId);
            if (customerContext is not null)
            {
                return Result.Ok<WorkspaceEntryPointModel?>(new WorkspaceEntryPointModel
                {
                    Kind = WorkspaceEntryPointKind.CustomerDashboard,
                    ContextId = customerContext.CustomerId,
                    CompanySlug = customerContext.ManagementCompanySlug,
                    CustomerSlug = customerContext.Slug
                });
            }
        }

        var defaultManagementCompanySlug = await GetDefaultManagementCompanySlugAsync(
            query.AppUserId,
            cancellationToken);
        if (!string.IsNullOrWhiteSpace(defaultManagementCompanySlug.Value))
        {
            return Result.Ok<WorkspaceEntryPointModel?>(new WorkspaceEntryPointModel
            {
                Kind = WorkspaceEntryPointKind.ManagementDashboard,
                CompanySlug = defaultManagementCompanySlug.Value
            });
        }

        var defaultResidentContext = await _uow.Residents.FirstActiveUserResidentContextAsync(
            query.AppUserId,
            cancellationToken);
        if (defaultResidentContext is not null)
        {
            return Result.Ok<WorkspaceEntryPointModel?>(new WorkspaceEntryPointModel
            {
                Kind = WorkspaceEntryPointKind.ResidentDashboard,
                ContextId = defaultResidentContext.ResidentId,
                CompanySlug = defaultResidentContext.ManagementCompanySlug,
                ResidentIdCode = defaultResidentContext.IdCode
            });
        }

        var defaultCustomerContext = (await _uow.Customers.ActiveUserCustomerContextsAsync(
                query.AppUserId,
                cancellationToken))
            .FirstOrDefault();
        if (defaultCustomerContext is not null)
        {
            return Result.Ok<WorkspaceEntryPointModel?>(new WorkspaceEntryPointModel
            {
                Kind = WorkspaceEntryPointKind.CustomerDashboard,
                ContextId = defaultCustomerContext.CustomerId,
                CompanySlug = defaultCustomerContext.ManagementCompanySlug,
                CustomerSlug = defaultCustomerContext.Slug
            });
        }

        return Result.Ok<WorkspaceEntryPointModel?>(null);
    }

    public Task<Result<WorkspaceSelectionAuthorizationModel>> AuthorizeContextSelectionAsync(
        AuthorizeContextSelectionQuery query,
        CancellationToken cancellationToken = default)
    {
        return AuthorizeContextSelectionAsync(
            query.AppUserId,
            query.ContextType,
            query.ContextId,
            cancellationToken);
    }

    private async Task<Result<WorkspaceSelectionAuthorizationModel>> AuthorizeContextSelectionAsync(
        Guid appUserId,
        string contextType,
        Guid? contextId,
        CancellationToken cancellationToken)
    {
        var normalizedType = contextType.Trim().ToLowerInvariant();

        switch (normalizedType)
        {
            case "management":
                if (!contextId.HasValue)
                {
                    return Result.Ok(new WorkspaceSelectionAuthorizationModel
                    {
                        Authorized = false,
                        ContextType = normalizedType
                    });
                }

                var managementCompany = await _uow.ManagementCompanies.ActiveUserManagementContextByCompanyIdAsync(
                    appUserId,
                    contextId.Value,
                    cancellationToken);

                if (managementCompany == null)
                {
                    return Result.Ok(new WorkspaceSelectionAuthorizationModel
                    {
                        Authorized = false,
                        ContextType = normalizedType
                    });
                }

                return Result.Ok(new WorkspaceSelectionAuthorizationModel
                {
                    Authorized = true,
                    ContextType = normalizedType,
                    ContextId = managementCompany.ManagementCompanyId,
                    Name = managementCompany.CompanyName,
                    ManagementCompanySlug = managementCompany.Slug
                });

            case "customer":
                if (!contextId.HasValue)
                {
                    return Result.Ok(new WorkspaceSelectionAuthorizationModel
                    {
                        Authorized = false,
                        ContextType = normalizedType
                    });
                }

                var customerContext = (await _uow.Customers.ActiveUserCustomerContextsAsync(
                        appUserId,
                        cancellationToken))
                    .FirstOrDefault(customer => customer.CustomerId == contextId.Value);

                return Result.Ok(new WorkspaceSelectionAuthorizationModel
                {
                    Authorized = customerContext is not null,
                    ContextType = normalizedType,
                    ContextId = customerContext?.CustomerId,
                    Name = customerContext?.Name,
                    ManagementCompanySlug = customerContext?.ManagementCompanySlug,
                    CustomerSlug = customerContext?.Slug
                });

            case "resident":
                if (!contextId.HasValue)
                {
                    return Result.Ok(new WorkspaceSelectionAuthorizationModel
                    {
                        Authorized = false,
                        ContextType = normalizedType
                    });
                }

                var residentContext = await _uow.Residents.FirstActiveUserResidentContextAsync(
                    appUserId,
                    cancellationToken);
                var hasResidentContext = residentContext is not null
                                         && residentContext.ResidentId == contextId.Value;

                return Result.Ok(new WorkspaceSelectionAuthorizationModel
                {
                    Authorized = hasResidentContext,
                    ContextType = normalizedType,
                    ContextId = hasResidentContext ? residentContext!.ResidentId : null,
                    Name = hasResidentContext ? residentContext!.DisplayName : null,
                    ManagementCompanySlug = hasResidentContext ? residentContext!.ManagementCompanySlug : null,
                    ResidentIdCode = hasResidentContext ? residentContext!.IdCode : null
                });

            default:
                return Result.Ok(new WorkspaceSelectionAuthorizationModel
                {
                    Authorized = false,
                    ContextType = normalizedType
                });
        }
    }

    private static readonly HashSet<string> CompanyUserManagerRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER"
    };

    private async Task<Result<bool>> HasManagementCompanyAccessAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken)
    {
        return Result.Ok(await _uow.ManagementCompanies.ActiveUserManagementContextExistsBySlugAsync(
            route.AppUserId,
            route.CompanySlug,
            cancellationToken));
    }
}
