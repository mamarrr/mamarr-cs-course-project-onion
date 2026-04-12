using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace App.Domain;

public class Resident : BaseEntity
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = default!;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = default!;

    [StringLength(20, MinimumLength = 1)]
    public string? IdCode { get; set; }

    [StringLength(20, MinimumLength = 1)]
    public string? PreferredLanguage { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid ManagementCompanyId { get; set; }
    public ManagementCompany? ManagementCompany { get; set; }

    public ICollection<ResidentUser>? ResidentUsers { get; set; }
    public ICollection<CustomerRepresentative>? CustomerRepresentatives { get; set; }
    public ICollection<Lease>? Leases { get; set; }
    public ICollection<ResidentContact>? ResidentContacts { get; set; }
    public ICollection<Ticket>? Tickets { get; set; }
}
