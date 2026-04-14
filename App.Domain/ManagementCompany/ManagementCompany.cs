using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace App.Domain;

public class ManagementCompany : BaseEntity
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = default!;

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Slug { get; set; } = default!;

    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string RegistryCode { get; set; } = default!;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string VatNumber { get; set; } = default!;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Phone { get; set; } = default!;

    [Required]
    [StringLength(300, MinimumLength = 1)]
    public string Address { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }

    public ICollection<ManagementCompanyUser>? ManagementCompanyUsers { get; set; }
    public ICollection<Customer>? Customers { get; set; }
    public ICollection<Resident>? Residents { get; set; }
    public ICollection<Vendor>? Vendors { get; set; }
    public ICollection<Contact>? Contacts { get; set; }
    public ICollection<Ticket>? Tickets { get; set; }
}
