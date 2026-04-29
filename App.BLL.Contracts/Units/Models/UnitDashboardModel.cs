namespace App.BLL.Contracts.Units.Models;

public sealed class UnitDashboardModel
{
    public UnitWorkspaceModel Workspace { get; init; } = new();
    public string Title { get; init; } = "Unit dashboard";
    public string SectionLabel { get; init; } = "Dashboard";
    public IReadOnlyList<string> Widgets { get; init; } = Array.Empty<string>();
}
