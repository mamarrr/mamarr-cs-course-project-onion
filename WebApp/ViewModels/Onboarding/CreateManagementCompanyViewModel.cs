using System.ComponentModel.DataAnnotations;
using App.Resources.Views;

namespace WebApp.ViewModels.Onboarding;

public class CreateManagementCompanyViewModel
{
    [Required(ErrorMessageResourceName = "RequiredField", ErrorMessageResourceType = typeof(UiText))]
    [StringLength(200, MinimumLength = 1, ErrorMessageResourceName = "StringLengthBetween", ErrorMessageResourceType = typeof(UiText))]
    [Display(Name = "Name", ResourceType = typeof(UiText))]
    public string Name { get; set; } = default!;

    [Required(ErrorMessageResourceName = "RequiredField", ErrorMessageResourceType = typeof(UiText))]
    [StringLength(255, MinimumLength = 1, ErrorMessageResourceName = "StringLengthBetween", ErrorMessageResourceType = typeof(UiText))]
    [Display(Name = "RegistryCode", ResourceType = typeof(UiText))]
    public string RegistryCode { get; set; } = default!;

    [Required(ErrorMessageResourceName = "RequiredField", ErrorMessageResourceType = typeof(UiText))]
    [StringLength(50, MinimumLength = 1, ErrorMessageResourceName = "StringLengthBetween", ErrorMessageResourceType = typeof(UiText))]
    [Display(Name = "VatNumber", ResourceType = typeof(UiText))]
    public string VatNumber { get; set; } = default!;

    [Required(ErrorMessageResourceName = "RequiredField", ErrorMessageResourceType = typeof(UiText))]
    [StringLength(200, MinimumLength = 1, ErrorMessageResourceName = "StringLengthBetween", ErrorMessageResourceType = typeof(UiText))]
    [EmailAddress(ErrorMessageResourceName = "InvalidEmailAddress", ErrorMessageResourceType = typeof(UiText))]
    [Display(Name = "Email", ResourceType = typeof(UiText))]
    public string Email { get; set; } = default!;

    [Required(ErrorMessageResourceName = "RequiredField", ErrorMessageResourceType = typeof(UiText))]
    [StringLength(50, MinimumLength = 1, ErrorMessageResourceName = "StringLengthBetween", ErrorMessageResourceType = typeof(UiText))]
    [Display(Name = "Phone", ResourceType = typeof(UiText))]
    public string Phone { get; set; } = default!;

    [Required(ErrorMessageResourceName = "RequiredField", ErrorMessageResourceType = typeof(UiText))]
    [StringLength(300, MinimumLength = 1, ErrorMessageResourceName = "StringLengthBetween", ErrorMessageResourceType = typeof(UiText))]
    [Display(Name = "Address", ResourceType = typeof(UiText))]
    public string Address { get; set; } = default!;
}
