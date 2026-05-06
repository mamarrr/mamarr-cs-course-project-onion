using System.Security.Claims;
using App.BLL.DTO.Customers.Commands;
using App.BLL.DTO.Customers.Models;
using App.BLL.DTO.Customers.Queries;
using App.DTO.v1.Management;
using App.DTO.v1.Shared;

namespace WebApp.Mappers.Api.Customers;

public class CompanyCustomerApiMapper
{
    public GetCompanyCustomersQuery ToQuery(
        string companySlug,
        ClaimsPrincipal user)
    {
        return new GetCompanyCustomersQuery
        {
            CompanySlug = companySlug,
            UserId = GetAppUserId(user)
        };
    }

    public CreateCustomerCommand ToCommand(
        string companySlug,
        CreateManagementCustomerRequestDto dto,
        ClaimsPrincipal user)
    {
        return new CreateCustomerCommand
        {
            CompanySlug = companySlug,
            UserId = GetAppUserId(user),
            Name = dto.Name,
            RegistryCode = dto.RegistryCode,
            BillingEmail = dto.BillingEmail,
            BillingAddress = dto.BillingAddress,
            Phone = dto.Phone
        };
    }

    public ManagementCustomersResponseDto ToResponseDto(IReadOnlyList<CustomerListItemModel> models)
    {
        return new ManagementCustomersResponseDto
        {
            Customers = models.Select(ToSummaryDto).ToList()
        };
    }

    public CreateManagementCustomerResponseDto ToCreateResponseDto(CompanyCustomerModel model)
    {
        return new CreateManagementCustomerResponseDto
        {
            CustomerId = model.CustomerId,
            CustomerSlug = model.CustomerSlug,
            RouteContext = CreateRouteContext(
                model.CompanySlug,
                model.CompanyName,
                model.CustomerSlug,
                model.Name)
        };
    }

    private static ManagementCustomerSummaryDto ToSummaryDto(CustomerListItemModel model)
    {
        return new ManagementCustomerSummaryDto
        {
            CustomerId = model.CustomerId,
            CustomerSlug = model.CustomerSlug,
            Name = model.Name,
            RegistryCode = model.RegistryCode,
            BillingEmail = model.BillingEmail,
            BillingAddress = model.BillingAddress,
            Phone = model.Phone,
            RouteContext = CreateRouteContext(
                model.CompanySlug,
                model.CompanyName,
                model.CustomerSlug,
                model.Name)
        };
    }

    private static ApiRouteContextDto CreateRouteContext(
        string companySlug,
        string companyName,
        string customerSlug,
        string customerName)
    {
        return new ApiRouteContextDto
        {
            CompanySlug = companySlug,
            CompanyName = companyName,
            CustomerSlug = customerSlug,
            CustomerName = customerName,
            CurrentSection = "customer-dashboard"
        };
    }

    private static Guid GetAppUserId(ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : Guid.Empty;
    }
}
