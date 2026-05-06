using App.BLL.DTO.Customers.Commands;
using App.BLL.DTO.Customers.Models;
using App.BLL.DTO.Customers.Queries;
using WebApp.ViewModels.Customer.CustomerProfile;

namespace WebApp.Mappers.Mvc.Customers;

public class CustomerProfileMvcMapper
{
    public GetCustomerProfileQuery ToQuery(
        string companySlug,
        string customerSlug,
        Guid appUserId)
    {
        return new GetCustomerProfileQuery
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            UserId = appUserId
        };
    }

    public UpdateCustomerProfileCommand ToCommand(
        string companySlug,
        string customerSlug,
        CustomerProfileEditViewModel edit,
        Guid appUserId)
    {
        return new UpdateCustomerProfileCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            UserId = appUserId,
            Name = edit.Name,
            RegistryCode = edit.RegistryCode,
            BillingEmail = edit.BillingEmail,
            BillingAddress = edit.BillingAddress,
            Phone = edit.Phone,
        };
    }

    public DeleteCustomerCommand ToDeleteCommand(
        string companySlug,
        string customerSlug,
        CustomerProfileEditViewModel edit,
        Guid appUserId)
    {
        return new DeleteCustomerCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            UserId = appUserId,
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
        };
    }

}
