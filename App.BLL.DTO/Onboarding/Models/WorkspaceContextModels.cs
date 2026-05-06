namespace App.BLL.Contracts.Onboarding.Models;

public class WorkspaceContextCatalogModel
{
    public IReadOnlyList<WorkspaceContextModel> Contexts { get; init; } = [];
    public WorkspaceContextModel? DefaultContext { get; init; }
}

public class WorkspaceContextModel
{
    public string ContextType { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
    public string? CompanySlug { get; init; }
    public string? CompanyName { get; init; }
    public Guid? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string? ResidentDisplayName { get; init; }
}
