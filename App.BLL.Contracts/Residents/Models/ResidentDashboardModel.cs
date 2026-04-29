namespace App.BLL.Contracts.Residents.Models;

public sealed class ResidentDashboardModel
{
    public ResidentWorkspaceModel Workspace { get; init; } = new();
    public string Title { get; init; } = "Resident dashboard";
    public string SectionLabel { get; init; } = "Dashboard";
    public IReadOnlyList<string> Widgets { get; init; } = Array.Empty<string>();
}
