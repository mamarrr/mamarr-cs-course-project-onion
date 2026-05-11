namespace App.DTO.v1.Portal.Tickets;

public class ContextTicketsDto : ManagementTicketsDto
{
    public string ContextName { get; set; } = string.Empty;
    public string? CustomerSlug { get; set; }
    public string? CustomerName { get; set; }
    public string? PropertySlug { get; set; }
    public string? PropertyName { get; set; }
    public string? UnitSlug { get; set; }
    public string? UnitName { get; set; }
    public string? ResidentIdCode { get; set; }
    public string? ResidentName { get; set; }
}
