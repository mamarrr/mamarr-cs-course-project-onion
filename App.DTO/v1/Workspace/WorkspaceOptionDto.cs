namespace App.DTO.v1.Workspace;

public class WorkspaceOptionDto
{
    public Guid Id { get; set; }
    public string ContextType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? ManagementCompanySlug { get; set; }
    public string Path { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public WorkspaceOptionPermissionsDto Permissions { get; set; } = new();
}
