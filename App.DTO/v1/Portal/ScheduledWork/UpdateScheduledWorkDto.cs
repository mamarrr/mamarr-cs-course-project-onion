using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Portal.ScheduledWork;

public class UpdateScheduledWorkDto
{
    public Guid VendorId { get; set; }
    public Guid WorkStatusId { get; set; }

    [Required]
    public DateTime ScheduledStart { get; set; }

    public DateTime? ScheduledEnd { get; set; }
    public DateTime? RealStart { get; set; }
    public DateTime? RealEnd { get; set; }

    [StringLength(4000)]
    public string? Notes { get; set; }
}
