using System.Security.Claims;
using App.BLL.Contracts.Customers.Models;
using App.BLL.Contracts.Customers.Queries;
using App.DTO.v1.Customer;
using App.DTO.v1.Shared;

namespace WebApp.Mappers.Api.Customers;

public class CustomerWorkspaceApiMapper
{
    public GetCustomerWorkspaceQuery ToQuery(
        string companySlug,
        string customerSlug,
        ClaimsPrincipal user)
    {
        return ToQuery(companySlug, customerSlug, GetAppUserId(user));
    }

    public GetCustomerWorkspaceQuery ToQuery(
        string companySlug,
        string customerSlug,
        Guid appUserId)
    {
        return new GetCustomerWorkspaceQuery
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            UserId = appUserId
        };
    }

    public CustomerDashboardResponseDto ToDashboardResponseDto(CustomerWorkspaceModel model)
    {
        return new CustomerDashboardResponseDto
        {
            Dashboard = new ApiDashboardDto
            {
                RouteContext = new ApiRouteContextDto
                {
                    CompanySlug = model.CompanySlug,
                    CompanyName = model.CompanyName,
                    CustomerSlug = model.CustomerSlug,
                    CustomerName = model.CustomerName,
                    CurrentSection = "customer-dashboard"
                },
                Title = "Customer dashboard",
                SectionLabel = "Dashboard",
                Widgets = Array.Empty<string>()
            }
        };
    }

    private static Guid GetAppUserId(ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : Guid.Empty;
    }
}
