using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Base.Domain;

namespace App.Domain;

public class Contact : BaseEntity
{
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string ContactValue { get; set; } = default!;
    public DateTime CreatedAt { get; set; }

    [Display(ResourceType = typeof(App.Resources.Domain.Contact), Name = nameof(App.Resources.Domain.Contact.Notes))]
    [Column(TypeName = "jsonb")]
    public LangStr? Notes { get; set; }

    public Guid ContactTypeId { get; set; }
    public ContactType? ContactType { get; set; }

    public Guid ManagementCompanyId { get; set; }
    public ManagementCompany? ManagementCompany { get; set; }

    public ICollection<VendorContact>? VendorContacts { get; set; }
    public ICollection<ResidentContact>? ResidentContacts { get; set; }
}
