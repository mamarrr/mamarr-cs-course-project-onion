using System.ComponentModel.DataAnnotations;
using Base.Domain;
using App.Domain.Identity;

namespace App.Domain;

public class WorkLog : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime? WorkStart { get; set; }
    public DateTime? WorkEnd { get; set; }
    [Range(typeof(decimal), "0", "99999999.99")]
    public decimal? Hours { get; set; }

    [Range(typeof(decimal), "0", "99999999.99")]
    public decimal? MaterialCost { get; set; }

    [Range(typeof(decimal), "0", "99999999.99")]
    public decimal? LaborCost { get; set; }

    [MinLength(1)]
    public string? Description { get; set; }

    public Guid AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    public Guid ScheduledWorkId { get; set; }
    public ScheduledWork? ScheduledWork { get; set; }
}
