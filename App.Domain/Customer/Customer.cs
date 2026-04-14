using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace App.Domain;

public class Customer : BaseEntity
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = default!;

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Slug { get; set; } = default!;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string RegistryCode { get; set; } = default!;

    [StringLength(200, MinimumLength = 1)]
    [EmailAddress]
    public string? BillingEmail { get; set; }

    [StringLength(255, MinimumLength = 1)]
    public string? BillingAddress { get; set; }

    [StringLength(50, MinimumLength = 1)]
    public string? Phone { get; set; }

    [MinLength(1)]
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid ManagementCompanyId { get; set; }
    public ManagementCompany? ManagementCompany { get; set; }

    public ICollection<Property>? Properties { get; set; }
    public ICollection<CustomerRepresentative>? CustomerRepresentatives { get; set; }
    public ICollection<Ticket>? Tickets { get; set; }
}
