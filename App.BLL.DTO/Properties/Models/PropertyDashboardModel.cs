namespace App.BLL.DTO.Properties.Models;

public class PropertyDashboardModel
{
    public PropertyWorkspaceModel Workspace { get; init; } = default!;
    public string Title { get; init; } = "Property dashboard";
    public string SectionLabel { get; init; } = "Dashboard";
    public IReadOnlyList<string> Widgets { get; init; } = Array.Empty<string>();
}
