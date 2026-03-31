using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels;

public class ListItemCreateViewModel
{
    [StringLength(128,MinimumLength = 1)]
    public string ItemDescription { get; set; } = default!;
 
    public string Summary { get; set; } = default!;

    public bool IsDone { get; set; }

}