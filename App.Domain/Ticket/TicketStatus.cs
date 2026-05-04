using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.DAL.Contracts;
using Base.Domain;

namespace App.Domain;

public class TicketStatus : BaseEntity, ILookUpEntity
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Code { get; set; } = default!;

    [Required]
    [Display(ResourceType = typeof(App.Resources.Domain.TicketStatus), Name = nameof(App.Resources.Domain.TicketStatus.Label))]
    [Column(TypeName = "jsonb")]
    public LangStr Label { get; set; } = default!;

    public ICollection<Ticket>? Tickets { get; set; }
}
