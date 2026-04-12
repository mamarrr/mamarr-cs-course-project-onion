using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace App.Domain;

public class TicketStatus : BaseEntity
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Code { get; set; } = default!;

    [Required]
    public LangStr Label { get; set; } = default!;

    public ICollection<Ticket>? Tickets { get; set; }
}
