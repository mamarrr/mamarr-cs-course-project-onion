namespace WebApp.UI.Workspace;

public sealed class WorkspaceIdentityViewModel
{
    public WorkspaceLevel Level { get; init; } = WorkspaceLevel.None;

    public string? ManagementCompanySlug { get; init; }

    public string? ManagementCompanyName { get; init; }

    public string? CustomerSlug { get; init; }

    public string? CustomerName { get; init; }

    public string? PropertySlug { get; init; }

    public string? PropertyName { get; init; }

    public string? UnitSlug { get; init; }

    public string? UnitName { get; init; }

    public string? ResidentIdCode { get; init; }

    public string? ResidentDisplayName { get; init; }

    public string? ResidentSupportingText { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public bool HasResidentContext { get; init; }
}
