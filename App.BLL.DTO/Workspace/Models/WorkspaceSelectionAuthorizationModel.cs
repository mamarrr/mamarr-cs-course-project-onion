namespace App.BLL.DTO.Workspace.Models;

public class WorkspaceSelectionAuthorizationModel
{
    public bool Authorized { get; init; }
    public string ContextType { get; init; } = default!;
    public Guid? ContextId { get; init; }
    public string? Name { get; init; }
    public string? ManagementCompanySlug { get; init; }
    public string? CustomerSlug { get; init; }
    public string? ResidentIdCode { get; init; }
}
