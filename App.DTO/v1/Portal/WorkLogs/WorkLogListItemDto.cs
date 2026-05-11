namespace App.DTO.v1.Portal.WorkLogs;

public class WorkLogListItemDto
{
    public Guid WorkLogId { get; set; }
    public Guid AppUserId { get; set; }
    public string AppUserName { get; set; } = string.Empty;
    public DateTime? WorkStart { get; set; }
    public DateTime? WorkEnd { get; set; }
    public decimal? Hours { get; set; }
    public decimal? MaterialCost { get; set; }
    public decimal? LaborCost { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Path { get; set; } = string.Empty;
}
