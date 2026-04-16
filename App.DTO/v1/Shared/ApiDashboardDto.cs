namespace App.DTO.v1.Shared;

public class ApiDashboardDto
{
    public ApiRouteContextDto RouteContext { get; set; } = new();
    public string Title { get; set; } = string.Empty;
    public string SectionLabel { get; set; } = string.Empty;
    public IReadOnlyList<string> Widgets { get; set; } = Array.Empty<string>();
}
