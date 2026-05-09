namespace App.BLL.DTO.Tickets.Models;

public class ContextTicketsModel : ManagementTicketsModel
{
    public string ContextName { get; init; } = string.Empty;
    public string? CustomerSlug { get; init; }
    public string? CustomerName { get; init; }
    public string? PropertySlug { get; init; }
    public string? PropertyName { get; init; }
    public string? UnitSlug { get; init; }
    public string? UnitName { get; init; }
    public string? ResidentIdCode { get; init; }
    public string? ResidentName { get; init; }
}
