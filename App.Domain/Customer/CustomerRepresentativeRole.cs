using System.ComponentModel.DataAnnotations;
using App.Contracts;
using Base.Domain;

namespace App.Domain;

public class CustomerRepresentativeRole : BaseEntity, ILookUpEntity
{
    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string Code { get; set; } = default!;

    [Required]
    public LangStr Label { get; set; } = default!;

    public ICollection<CustomerRepresentative>? CustomerRepresentatives { get; set; }
}
