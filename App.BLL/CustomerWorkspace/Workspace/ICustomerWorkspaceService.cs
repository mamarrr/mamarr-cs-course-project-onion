namespace App.BLL.CustomerWorkspace.Workspace;

public interface ICustomerWorkspaceService
{
    Task<CustomerWorkspaceAuthorizationResult> AuthorizeAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);

    Task<CustomerWorkspaceDashboardAccessResult> AuthorizeCustomerContextAsync(
        Guid appUserId,
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken = default);

    Task<CustomerWorkspaceDashboardAccessResult> ResolveDashboardAccessAsync(
        Guid appUserId,
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken = default);

    Task<CompanyCustomerListResult> ListAsync(
        CustomerWorkspaceAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<CustomerCreateResult> CreateAsync(
        CustomerWorkspaceAuthorizedContext context,
        CustomerCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerPropertyListResult> ListPropertiesAsync(
        CustomerWorkspaceDashboardContext context,
        CancellationToken cancellationToken = default);

    Task<PropertyCreateResult> CreatePropertyAsync(
        CustomerWorkspaceDashboardContext context,
        PropertyCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<PropertyDashboardAccessResult> ResolvePropertyDashboardContextAsync(
        CustomerWorkspaceDashboardContext context,
        string propertySlug,
        CancellationToken cancellationToken = default);
}
