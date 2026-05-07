using Base.Domain;

namespace App.BLL.DTO.WorkLogs;

public class WorkLogBllDto : BaseEntity
{
    public Guid ScheduledWorkId { get; set; }
    public Guid AppUserId { get; set; }
    public DateTime? WorkStart { get; set; }
    public DateTime? WorkEnd { get; set; }
    public decimal? Hours { get; set; }
    public decimal? MaterialCost { get; set; }
    public decimal? LaborCost { get; set; }
    public string? Description { get; set; }
}
