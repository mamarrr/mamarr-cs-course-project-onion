namespace App.BLL.DTO.Workspace.Queries;

public class ResolveWorkspaceEntryPointQuery
{
    public Guid AppUserId { get; init; }
    public RememberedWorkspaceContext RememberedContext { get; init; } = new();
}

public class RememberedWorkspaceContext
{
    public string? ContextType { get; init; }
    public string? ManagementCompanySlug { get; init; }
    public string? CustomerId { get; init; }
}
