using App.BLL.DTO.Customers.Queries;

namespace WebApp.Mappers.Mvc.Customers;

public class CustomerWorkspaceMvcMapper
{
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
}
