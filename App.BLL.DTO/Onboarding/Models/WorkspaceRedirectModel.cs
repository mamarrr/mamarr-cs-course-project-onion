namespace App.BLL.DTO.Onboarding.Models;

public enum WorkspaceRedirectDestination
{
    Home,
    ManagementDashboard,
    CustomerDashboard,
    ResidentDashboard
}

public class WorkspaceRedirectModel
{
    public required WorkspaceRedirectDestination Destination { get; init; }
    public string? CompanySlug { get; init; }
}
