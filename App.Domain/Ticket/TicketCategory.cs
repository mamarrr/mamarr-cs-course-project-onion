using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Contracts;
using Base.Domain;

namespace App.Domain;

public class TicketCategory : BaseEntity, ILookUpEntity
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Code { get; set; } = default!;

    [Required]
    [Display(ResourceType = typeof(App.Resources.Domain.TicketCategory), Name = nameof(App.Resources.Domain.TicketCategory.Label))]
    [Column(TypeName = "jsonb")]
    public LangStr Label { get; set; } = default!;

    public ICollection<Ticket>? Tickets { get; set; }
    public ICollection<VendorTicketCategory>? VendorTicketCategories { get; set; }
}
