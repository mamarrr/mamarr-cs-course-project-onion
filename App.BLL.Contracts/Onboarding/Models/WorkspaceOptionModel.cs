namespace App.BLL.Contracts.Onboarding.Models;

public sealed class WorkspaceOptionModel
{
    public Guid Id { get; init; }
    public string ContextType { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Slug { get; init; }
    public bool IsDefault { get; init; }
}
