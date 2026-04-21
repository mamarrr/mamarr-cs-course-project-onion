using App.BLL.CustomerWorkspace.Workspace;

namespace App.BLL.PropertyWorkspace.Properties;

public interface IPropertyWorkspaceService
{
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

