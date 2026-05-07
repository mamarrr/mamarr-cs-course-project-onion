namespace App.BLL.DTO.Workspace.Queries;

public class AuthorizeContextSelectionQuery
{
    public Guid AppUserId { get; init; }
    public string ContextType { get; init; } = default!;
    public Guid? ContextId { get; init; }
}
