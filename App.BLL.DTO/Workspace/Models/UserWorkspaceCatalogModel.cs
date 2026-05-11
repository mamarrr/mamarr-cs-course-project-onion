namespace App.BLL.DTO.Workspace.Models;

public class UserWorkspaceCatalogModel
{
    public IReadOnlyList<WorkspaceOptionModel> ManagementCompanies { get; init; } = [];
    public IReadOnlyList<WorkspaceOptionModel> Customers { get; init; } = [];
    public IReadOnlyList<WorkspaceOptionModel> Residents { get; init; } = [];
    public WorkspaceOptionModel? DefaultContext { get; init; }
}
