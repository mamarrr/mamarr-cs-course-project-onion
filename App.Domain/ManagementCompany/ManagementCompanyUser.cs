using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Base.Domain;
using App.Domain.Identity;
using Base.Contracts;

namespace App.Domain;

public class ManagementCompanyUser : BaseEntity, IManagementCompanyId
{
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    [Required]
    [Display(ResourceType = typeof(App.Resources.Domain.ManagementCompanyUser), Name = nameof(App.Resources.Domain.ManagementCompanyUser.JobTitle))]
    [Column(TypeName = "jsonb")]
    public LangStr JobTitle { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid ManagementCompanyId { get; set; }
    public ManagementCompany? ManagementCompany { get; set; }

    public Guid AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    public Guid ManagementCompanyRoleId { get; set; }
    public ManagementCompanyRole? ManagementCompanyRole { get; set; }
}
