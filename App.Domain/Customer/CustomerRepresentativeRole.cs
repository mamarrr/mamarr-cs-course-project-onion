using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Contracts;
using Base.Domain;

namespace App.Domain;

public class CustomerRepresentativeRole : BaseEntity, ILookUpEntity
{
    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string Code { get; set; } = default!;

    [Required]
    [Display(ResourceType = typeof(App.Resources.Domain.CustomerRepresentativeRole), Name = nameof(App.Resources.Domain.CustomerRepresentativeRole.Label))]
    [Column(TypeName = "jsonb")]
    public LangStr Label { get; set; } = default!;

    public ICollection<CustomerRepresentative>? CustomerRepresentatives { get; set; }
}
