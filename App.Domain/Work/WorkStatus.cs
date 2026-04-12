using System.ComponentModel.DataAnnotations;
using App.Contracts;
using Base.Domain;

namespace App.Domain;

public class WorkStatus : BaseEntity, ILookUpEntity
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Code { get; set; } = default!;

    [Required]
    public LangStr Label { get; set; } = default!;

    public ICollection<ScheduledWork>? ScheduledWorks { get; set; }
}
