using System.ComponentModel.DataAnnotations;
using App.Domain;

namespace WebApp.ViewModels;

public class ContactTypeCreateViewModel
{
    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string Code { get; set; } = default!;

    [Required]
    public string Label { get; set; } = default!;
}
