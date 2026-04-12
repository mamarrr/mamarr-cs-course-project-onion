using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace App.Domain;

public class CustomerRepresentativeRole : BaseEntity
{
    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string Code { get; set; } = default!;

    [Required]
    public LangStr Label { get; set; } = default!;

    public ICollection<CustomerRepresentative>? CustomerRepresentatives { get; set; }
}
