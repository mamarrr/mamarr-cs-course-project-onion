using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.DAL.Contracts;
using Base.Domain;

namespace App.Domain;

public class WorkStatus : BaseEntity, ILookUpEntity
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Code { get; set; } = default!;

    [Required]
    [Display(ResourceType = typeof(App.Resources.Domain.WorkStatus), Name = nameof(App.Resources.Domain.WorkStatus.Label))]
    [Column(TypeName = "jsonb")]
    public LangStr Label { get; set; } = default!;

    public ICollection<ScheduledWork>? ScheduledWorks { get; set; }
}
