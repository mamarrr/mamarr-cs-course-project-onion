using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.DAL.Contracts;
using Base.Domain;

namespace App.Domain;

public class ManagementCompanyRole : BaseEntity, ILookUpEntity
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Code { get; set; } = default!;

    [Required]
    [Display(ResourceType = typeof(App.Resources.Domain.ManagementCompanyRole), Name = nameof(App.Resources.Domain.ManagementCompanyRole.Label))]
    [Column(TypeName = "jsonb")]
    public LangStr Label { get; set; } = default!;

    public ICollection<ManagementCompanyUser>? ManagementCompanyUsers { get; set; }
    public ICollection<ManagementCompanyJoinRequest>? ManagementCompanyJoinRequests { get; set; }
}
