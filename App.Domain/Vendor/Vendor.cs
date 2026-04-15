using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Base.Domain;

namespace App.Domain;

public class Vendor : BaseEntity
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = default!;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string RegistryCode { get; set; } = default!;

    [Required]
    [Display(ResourceType = typeof(App.Resources.Domain.Vendor), Name = nameof(App.Resources.Domain.Vendor.Notes))]
    [Column(TypeName = "jsonb")]
    public LangStr Notes { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid ManagementCompanyId { get; set; }
    public ManagementCompany? ManagementCompany { get; set; }

    public ICollection<VendorContact>? VendorContacts { get; set; }
    public ICollection<VendorTicketCategory>? VendorTicketCategories { get; set; }
    public ICollection<Ticket>? Tickets { get; set; }
    public ICollection<ScheduledWork>? ScheduledWorks { get; set; }
}
