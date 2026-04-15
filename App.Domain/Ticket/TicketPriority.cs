using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Contracts;
using Base.Domain;

namespace App.Domain;

public class TicketPriority : BaseEntity, ILookUpEntity
{
    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string Code { get; set; } = default!;

    [Required]
    [Display(ResourceType = typeof(App.Resources.Domain.TicketPriority), Name = nameof(App.Resources.Domain.TicketPriority.Label))]
    [Column(TypeName = "jsonb")]
    public LangStr Label { get; set; } = default!;

    public ICollection<Ticket>? Tickets { get; set; }
}
