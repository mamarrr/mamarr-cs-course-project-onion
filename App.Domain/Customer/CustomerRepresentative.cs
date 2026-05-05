using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Base.Contracts;
using Base.Domain;

namespace App.Domain;

public class CustomerRepresentative : BaseEntity, ICustomerId, IHasCreatedAtMeta
{
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public DateTime CreatedAt { get; set; }
    [Display(ResourceType = typeof(App.Resources.Domain.CustomerRepresentative), Name = nameof(App.Resources.Domain.CustomerRepresentative.Notes))]
    [Column(TypeName = "jsonb")]
    public LangStr? Notes { get; set; }

    public Guid CustomerRepresentativeRoleId { get; set; }
    public CustomerRepresentativeRole? CustomerRepresentativeRole { get; set; }

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid ResidentId { get; set; }
    public Resident? Resident { get; set; }
}
