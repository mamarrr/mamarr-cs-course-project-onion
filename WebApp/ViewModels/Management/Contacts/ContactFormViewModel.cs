using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApp.ViewModels.Management.Contacts;

public class ContactFormViewModel
{
    [Display(Name = "Contacts", ResourceType = typeof(UiText))]
    public Guid ContactTypeId { get; set; }

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(255, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "Contacts", ResourceType = typeof(UiText))]
    public string ContactValue { get; set; } = default!;

    [StringLength(4000, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "Notes", ResourceType = typeof(UiText))]
    public string? Notes { get; set; }

    public IReadOnlyList<SelectListItem> ContactTypes { get; set; } = Array.Empty<SelectListItem>();
}
