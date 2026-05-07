using Base.Domain;

namespace App.DAL.DTO.WorkLogs;

public class WorkLogDalDto : BaseEntity
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

public class WorkLogListItemDalDto
{
    public Guid Id { get; init; }
    public Guid ScheduledWorkId { get; init; }
    public Guid AppUserId { get; init; }
    public string AppUserName { get; init; } = default!;
    public DateTime? WorkStart { get; init; }
    public DateTime? WorkEnd { get; init; }
    public decimal? Hours { get; init; }
    public decimal? MaterialCost { get; init; }
    public decimal? LaborCost { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class WorkLogTotalsDalDto
{
    public int Count { get; init; }
    public decimal Hours { get; init; }
    public decimal MaterialCost { get; init; }
    public decimal LaborCost { get; init; }
    public decimal TotalCost => MaterialCost + LaborCost;
}
