namespace App.BLL.Contracts.Onboarding.Models;

public enum WorkspaceRedirectDestination
{
    Home,
    ManagementDashboard,
    CustomerDashboard,
    ResidentDashboard
}

public sealed class WorkspaceRedirectModel
{
    public required WorkspaceRedirectDestination Destination { get; init; }
    public string? CompanySlug { get; init; }
}
