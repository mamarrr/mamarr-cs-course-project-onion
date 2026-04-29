using System.Security.Claims;
using App.BLL.Contracts.Customers.Commands;
using App.BLL.Contracts.Customers.Models;
using App.BLL.Contracts.Customers.Queries;
using App.DTO.v1.Customer;
using App.DTO.v1.Shared;

namespace WebApp.Mappers.Api.Customers;

public sealed class CustomerProfileApiMapper
{
    public GetCustomerProfileQuery ToQuery(
        string companySlug,
        string customerSlug,
        ClaimsPrincipal user)
    {
        return new GetCustomerProfileQuery
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            UserId = GetAppUserId(user)
        };
    }

    public UpdateCustomerProfileCommand ToCommand(
        string companySlug,
        string customerSlug,
        UpdateCustomerProfileRequestDto dto,
        ClaimsPrincipal user)
    {
        return new UpdateCustomerProfileCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            UserId = GetAppUserId(user),
            Name = dto.Name,
            RegistryCode = dto.RegistryCode,
            BillingEmail = dto.BillingEmail,
            BillingAddress = dto.BillingAddress,
            Phone = dto.Phone,
            IsActive = dto.IsActive
        };
    }

    public DeleteCustomerCommand ToCommand(
        string companySlug,
        string customerSlug,
        DeleteCustomerProfileRequestDto dto,
        ClaimsPrincipal user)
    {
        return new DeleteCustomerCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            UserId = GetAppUserId(user),
            ConfirmationName = dto.ConfirmationName
        };
    }

    public CustomerProfileResponseDto ToResponseDto(CustomerProfileModel model)
    {
        return new CustomerProfileResponseDto
        {
            Profile = new CustomerProfileDto
            {
                CustomerId = model.Id,
                CustomerSlug = model.Slug,
                Name = model.Name,
                RegistryCode = model.RegistryCode,
                BillingEmail = model.BillingEmail,
                BillingAddress = model.BillingAddress,
                Phone = model.Phone,
                IsActive = model.IsActive,
                RouteContext = new ApiRouteContextDto
                {
                    CompanySlug = model.CompanySlug,
                    CompanyName = model.CompanyName,
                    CustomerSlug = model.Slug,
                    CustomerName = model.Name,
                    CurrentSection = "customer-profile"
                }
            }
        };
    }

    private static Guid GetAppUserId(ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : Guid.Empty;
    }
}
