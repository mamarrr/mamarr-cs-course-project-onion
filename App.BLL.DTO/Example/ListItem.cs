using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace App.BLL.DTO;

public class ListItem: BaseEntity
{
    [StringLength(128,MinimumLength = 1)]
    public string ItemDescription { get; set; } = default!;
 
    public string Summary { get; set; } = default!;

    public bool IsDone { get; set; }
}