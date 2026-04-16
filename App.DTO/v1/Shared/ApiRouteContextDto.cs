namespace App.DTO.v1.Shared;

public class ApiRouteContextDto
{
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? CustomerSlug { get; set; }
    public string? CustomerName { get; set; }
    public string? PropertySlug { get; set; }
    public string? PropertyName { get; set; }
    public string? UnitSlug { get; set; }
    public string? UnitName { get; set; }
    public string? ResidentIdCode { get; set; }
    public string? ResidentDisplayName { get; set; }
    public string CurrentSection { get; set; } = string.Empty;
}
