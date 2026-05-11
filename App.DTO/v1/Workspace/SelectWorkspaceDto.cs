namespace App.DTO.v1.Workspace;

public class SelectWorkspaceDto
{
    public string ContextType { get; set; } = string.Empty;
    public Guid ContextId { get; set; }
}
