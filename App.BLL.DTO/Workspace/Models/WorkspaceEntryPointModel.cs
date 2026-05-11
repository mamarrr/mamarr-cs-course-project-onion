namespace App.BLL.DTO.Workspace.Models;

public enum WorkspaceEntryPointKind
{
    ManagementDashboard,
    CustomerDashboard,
    ResidentDashboard
}

public class WorkspaceEntryPointModel
{
    public required WorkspaceEntryPointKind Kind { get; init; }
    public Guid? ContextId { get; init; }
    public string? CompanySlug { get; init; }
    public string? CustomerSlug { get; init; }
    public string? ResidentIdCode { get; init; }
}
