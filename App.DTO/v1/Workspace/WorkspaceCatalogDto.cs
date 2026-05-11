namespace App.DTO.v1.Workspace;

public class WorkspaceCatalogDto
{
    public IReadOnlyList<WorkspaceOptionDto> ManagementCompanies { get; set; } = [];
    public IReadOnlyList<WorkspaceOptionDto> Customers { get; set; } = [];
    public IReadOnlyList<WorkspaceOptionDto> Residents { get; set; } = [];
    public WorkspaceOptionDto? DefaultContext { get; set; }
}
