using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Domain.Identity;
using Base.Contracts;
using Base.Domain;

namespace App.Domain;

public class ManagementCompanyJoinRequest : BaseEntity, IManagementCompanyId
{
    public Guid AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    public Guid ManagementCompanyId { get; set; }
    public ManagementCompany? ManagementCompany { get; set; }

    public Guid RequestedManagementCompanyRoleId { get; set; }
    public ManagementCompanyRole? RequestedManagementCompanyRole { get; set; }

    public Guid ManagementCompanyJoinRequestStatusId { get; set; }
    public ManagementCompanyJoinRequestStatus? ManagementCompanyJoinRequestStatus { get; set; }

    [StringLength(2000)]
    [Display(ResourceType = typeof(App.Resources.Domain.ManagementCompanyJoinRequest), Name = nameof(App.Resources.Domain.ManagementCompanyJoinRequest.Message))]
    [Column(TypeName = "jsonb")]
    public LangStr? Message { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public Guid? ResolvedByAppUserId { get; set; }
    public AppUser? ResolvedByAppUser { get; set; }
}

