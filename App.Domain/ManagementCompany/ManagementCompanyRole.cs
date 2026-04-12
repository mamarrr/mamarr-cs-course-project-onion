using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace App.Domain;

public class ManagementCompanyRole : BaseEntity
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Code { get; set; } = default!;

    [Required]
    public LangStr Label { get; set; } = default!;

    public ICollection<ManagementCompanyUser>? ManagementCompanyUsers { get; set; }
}
