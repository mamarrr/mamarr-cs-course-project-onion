namespace App.BLL.DTO.Residents.Models;

public class ResidentDashboardModel
{
    public ResidentWorkspaceModel Workspace { get; init; } = new();
    public string Title { get; init; } = "Resident dashboard";
    public string SectionLabel { get; init; } = "Dashboard";
    public IReadOnlyList<string> Widgets { get; init; } = Array.Empty<string>();
}
