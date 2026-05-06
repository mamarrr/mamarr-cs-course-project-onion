using App.BLL.Contracts.Onboarding;
using App.BLL.DTO.Onboarding.Models;
using App.DAL.Contracts;
using FluentResults;

namespace App.BLL.Services.Onboarding.WorkspaceContext;

public class WorkspaceContextService : IWorkspaceContextService
{
    private readonly IAppUOW _uow;
    private readonly IAccountOnboardingService _accountOnboardingService;

    public WorkspaceContextService(IAppUOW uow, IAccountOnboardingService accountOnboardingService)
    {
        _uow = uow;
        _accountOnboardingService = accountOnboardingService;
    }

    public async Task<Result<WorkspaceContextCatalogModel>> GetContextsAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        var contexts = new List<WorkspaceContextModel>();
        var defaultManagementCompanySlug = await _accountOnboardingService.GetDefaultManagementCompanySlugAsync(
            appUserId,
            cancellationToken);

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
