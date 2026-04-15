using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApp.ViewModels.Onboarding;

public class JoinManagementCompanyViewModel
{
    public string Title { get; init; } = UiText.TitleJoinManagementCompany;
    public string Heading { get; init; } = UiText.HeadingJoinManagementCompany;

    [Required]
    [StringLength(255, MinimumLength = 1)]
    [Display(Name = nameof(UiText.CompanyRegistryCode), ResourceType = typeof(UiText))]
    public string RegistryCode { get; set; } = default!;

    [Required]
    [Display(Name = nameof(UiText.RequestedRole), ResourceType = typeof(UiText))]
    public Guid? RequestedRoleId { get; set; }

    [StringLength(2000)]
    [Display(Name = nameof(UiText.MessageOptional), ResourceType = typeof(UiText))]
    public string? Message { get; set; }

    public IReadOnlyList<SelectListItem> AvailableRoles { get; set; } = Array.Empty<SelectListItem>();
}
