namespace App.BLL.DTO.Workspace.Models;

public enum WorkspaceRedirectDestination
{
    ManagementDashboard,
    CustomerDashboard,
    ResidentDashboard
}

public class WorkspaceRedirectModel
{
    public required WorkspaceRedirectDestination Destination { get; init; }
    public string? CompanySlug { get; init; }
}
