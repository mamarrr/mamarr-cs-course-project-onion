using App.BLL.Contracts.Onboarding;
using App.BLL.DTO.Onboarding.Models;
using App.BLL.DTO.Onboarding.Queries;
using App.DAL.Contracts;
using FluentResults;

namespace App.BLL.Services.Onboarding.WorkspaceCatalog;

public class UserWorkspaceCatalogService : IWorkspaceCatalogService
{
    private static readonly HashSet<string> CompanyUserManagerRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER"
    };

    private readonly IAppUOW _uow;

    public UserWorkspaceCatalogService(IAppUOW uow)
    {
        _uow = uow;
    }

    public async Task<Result<WorkspaceCatalogModel>> GetWorkspaceCatalogAsync(
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
}
