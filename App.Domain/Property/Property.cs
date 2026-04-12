using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace App.Domain;

public class Property : BaseEntity
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Label { get; set; } = default!;

    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string AddressLine { get; set; } = default!;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string City { get; set; } = default!;

    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string PostalCode { get; set; } = default!;

    [MinLength(1)]
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid PropertyTypeId { get; set; }
    public PropertyType? PropertyType { get; set; }

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public ICollection<Unit>? Units { get; set; }
    public ICollection<Ticket>? Tickets { get; set; }
}
