using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Contracts;
using Base.Domain;

namespace App.Domain;

public class ManagementCompanyJoinRequestStatus : BaseEntity, ILookUpEntity
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Code { get; set; } = default!;

    [Required]
    [Column(TypeName = "jsonb")]
    public LangStr Label { get; set; } = default!;

    public ICollection<ManagementCompanyJoinRequest>? ManagementCompanyJoinRequests { get; set; }
}

