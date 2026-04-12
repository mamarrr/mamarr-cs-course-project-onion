using System.ComponentModel.DataAnnotations;
using Base.Domain;
using App.Domain.Identity;

namespace App.Domain;

public class ManagementCompanyUser : BaseEntity
{
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string JobTitle { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid ManagementCompanyId { get; set; }
    public ManagementCompany? ManagementCompany { get; set; }

    public Guid AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    public Guid ManagementCompanyRoleId { get; set; }
    public ManagementCompanyRole? ManagementCompanyRole { get; set; }
}
