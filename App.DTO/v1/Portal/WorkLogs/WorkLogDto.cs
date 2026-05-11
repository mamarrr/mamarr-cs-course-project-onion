namespace App.DTO.v1.Portal.WorkLogs;

public class WorkLogDto
{
    public Guid WorkLogId { get; set; }
    public Guid ScheduledWorkId { get; set; }
    public Guid AppUserId { get; set; }
    public DateTime? WorkStart { get; set; }
    public DateTime? WorkEnd { get; set; }
    public decimal? Hours { get; set; }
    public decimal? MaterialCost { get; set; }
    public decimal? LaborCost { get; set; }
    public string? Description { get; set; }
    public string Path { get; set; } = string.Empty;
}
