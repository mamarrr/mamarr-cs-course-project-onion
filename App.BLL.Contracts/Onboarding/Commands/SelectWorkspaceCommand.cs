namespace App.BLL.Contracts.Onboarding.Commands;

public sealed class SelectWorkspaceCommand
{
    public Guid AppUserId { get; init; }
    public string ContextType { get; init; } = default!;
    public Guid? ContextId { get; init; }
}
