using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Contracts;
using Base.Domain;

namespace App.Domain;

public class PropertyType : BaseEntity, ILookUpEntity
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Code { get; set; } = default!;

    [Required]
    [Display(ResourceType = typeof(App.Resources.Domain.PropertyType), Name = nameof(App.Resources.Domain.PropertyType.Label))]
    [Column(TypeName = "jsonb")]
    public LangStr Label { get; set; } = default!;

    public ICollection<Property>? Properties { get; set; }
}
