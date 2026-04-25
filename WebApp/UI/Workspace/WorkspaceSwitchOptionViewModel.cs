namespace WebApp.UI.Workspace;

public sealed class WorkspaceSwitchOptionViewModel
{
    public Guid Id { get; init; }

    public string Slug { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public bool IsCurrent { get; init; }

    public string? Url { get; init; }
}
