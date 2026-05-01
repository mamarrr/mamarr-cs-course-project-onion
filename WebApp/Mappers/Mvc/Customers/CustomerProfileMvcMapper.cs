using System.Security.Claims;
using App.BLL.Contracts.Customers.Commands;
using App.BLL.Contracts.Customers.Models;
using App.BLL.Contracts.Customers.Queries;
using WebApp.ViewModels.Customer.CustomerProfile;

namespace WebApp.Mappers.Mvc.Customers;

public class CustomerProfileMvcMapper
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
        CustomerProfileEditViewModel edit,
        ClaimsPrincipal user)
    {
        return new UpdateCustomerProfileCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            UserId = GetAppUserId(user),
            Name = edit.Name,
            RegistryCode = edit.RegistryCode,
            BillingEmail = edit.BillingEmail,
            BillingAddress = edit.BillingAddress,
            Phone = edit.Phone,
            IsActive = edit.IsActive
        };
    }

    public DeleteCustomerCommand ToDeleteCommand(
        string companySlug,
        string customerSlug,
        CustomerProfileEditViewModel edit,
        ClaimsPrincipal user)
    {
        return new DeleteCustomerCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            UserId = GetAppUserId(user),
            ConfirmationName = edit.DeleteConfirmation ?? string.Empty
        };
    }

    public CustomerProfileEditViewModel ToEditViewModel(CustomerProfileModel profile)
    {
        return new CustomerProfileEditViewModel
        {
            Name = profile.Name,
            RegistryCode = profile.RegistryCode,
            BillingEmail = profile.BillingEmail,
            BillingAddress = profile.BillingAddress,
            Phone = profile.Phone,
            IsActive = profile.IsActive
        };
    }

    private static Guid GetAppUserId(ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : Guid.Empty;
    }
}
