namespace App.DTO.v1.Portal.Dashboards;

public class DashboardUnitPreviewDto
{
    public Guid UnitId { get; set; }
    public string UnitSlug { get; set; } = string.Empty;
    public string UnitNr { get; set; } = string.Empty;
    public int? FloorNr { get; set; }
    public decimal? SizeM2 { get; set; }
    public bool HasActiveLease { get; set; }
    public string? CurrentResidentName { get; set; }
    public int OpenTicketCount { get; set; }
    public string Path { get; set; } = string.Empty;
    public string EditPath { get; set; } = string.Empty;
}
