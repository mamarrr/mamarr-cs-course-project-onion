using System.ComponentModel.DataAnnotations;

namespace App.DTO.v1.Portal.ScheduledWork;

public class ScheduledWorkActionDto
{
    [Required]
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;
}
