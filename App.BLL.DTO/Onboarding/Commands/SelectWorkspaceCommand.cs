namespace App.BLL.DTO.Onboarding.Commands;

public class SelectWorkspaceCommand
{
    public Guid AppUserId { get; init; }
    public string ContextType { get; init; } = default!;
    public Guid? ContextId { get; init; }
}
