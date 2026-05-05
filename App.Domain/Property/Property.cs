using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Base.Contracts;
using Base.Domain;

namespace App.Domain;

public class Property : BaseEntity, ICustomerId, IHasCreatedAtMeta
{
    [Required]
    [Display(ResourceType = typeof(App.Resources.Domain.Property), Name = nameof(App.Resources.Domain.Property.Label))]
    [Column(TypeName = "jsonb")]
    public LangStr Label { get; set; } = default!;
    
    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Slug { get; set; } = default!;

    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string AddressLine { get; set; } = default!;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string City { get; set; } = default!;

    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string PostalCode { get; set; } = default!;

    [Display(ResourceType = typeof(App.Resources.Domain.Property), Name = nameof(App.Resources.Domain.Property.Notes))]
    [Column(TypeName = "jsonb")]
    public LangStr? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid PropertyTypeId { get; set; }
    public PropertyType? PropertyType { get; set; }

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public ICollection<Unit>? Units { get; set; }
    public ICollection<Ticket>? Tickets { get; set; }
}
