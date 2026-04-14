using System.ComponentModel.DataAnnotations;
using App.Domain.Identity;
using Base.Domain;

namespace App.Domain;

public class ManagementCompanyJoinRequest : BaseEntity
{
    public Guid AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    public Guid ManagementCompanyId { get; set; }
    public ManagementCompany? ManagementCompany { get; set; }

    public Guid RequestedManagementCompanyRoleId { get; set; }
    public ManagementCompanyRole? RequestedManagementCompanyRole { get; set; }

    [Required]
    [StringLength(32, MinimumLength = 1)]
    public string Status { get; set; } = ManagementCompanyJoinRequestStatus.Pending;

    [StringLength(2000)]
    public string? Message { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public Guid? ResolvedByAppUserId { get; set; }
    public AppUser? ResolvedByAppUser { get; set; }
}

