using App.BLL.Contracts.Onboarding.Models;
using App.DTO.v1.Onboarding;
using App.DTO.v1.Shared;

namespace WebApp.ApiControllers.Shared;

public interface IApiOnboardingRouteContextMapper
{
    OnboardingContextSummaryDto MapContext(ApiOnboardingContextModel entry);
    OnboardingContextsResponseDto MapCatalog(ApiOnboardingContextCatalogModel catalog);
    ApiRouteContextDto CreateManagementCompanyRouteContext(string companySlug, string companyName);
}

public class ApiOnboardingRouteContextMapper : IApiOnboardingRouteContextMapper
{
    public OnboardingContextSummaryDto MapContext(ApiOnboardingContextModel entry)
    {
        return new OnboardingContextSummaryDto
        {
            ContextType = entry.ContextType,
            Label = entry.Label,
            IsDefault = entry.IsDefault,
            RouteContext = new ApiRouteContextDto
            {
                CompanySlug = entry.CompanySlug ?? string.Empty,
                CompanyName = entry.CompanyName ?? string.Empty,
                CustomerName = entry.CustomerName,
                ResidentDisplayName = entry.ResidentDisplayName,
                CurrentSection = ApiRouteSections.FromContextType(entry.ContextType)
            }
        };
    }

    public OnboardingContextsResponseDto MapCatalog(ApiOnboardingContextCatalogModel catalog)
    {
        return new OnboardingContextsResponseDto
        {
            Contexts = catalog.Contexts.Select(MapContext).ToList(),
            DefaultContext = catalog.DefaultContext == null ? null : MapContext(catalog.DefaultContext)
        };
    }

    public ApiRouteContextDto CreateManagementCompanyRouteContext(string companySlug, string companyName)
    {
        return new ApiRouteContextDto
        {
            CompanySlug = companySlug,
            CompanyName = companyName,
            CurrentSection = ApiRouteSections.ManagementDashboard
        };
    }
}
