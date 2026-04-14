using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApp.ViewModels.Onboarding;

public class JoinManagementCompanyViewModel
{
    public string Title { get; init; } = "Join management company";
    public string Heading { get; init; } = "Management company employee onboarding";

    [Required]
    [StringLength(255, MinimumLength = 1)]
    [Display(Name = "Company registry code")]
    public string RegistryCode { get; set; } = default!;

    [Required]
    [Display(Name = "Requested role")]
    public Guid? RequestedRoleId { get; set; }

    [StringLength(2000)]
    [Display(Name = "Message (optional)")]
    public string? Message { get; set; }

    public IReadOnlyList<SelectListItem> AvailableRoles { get; set; } = Array.Empty<SelectListItem>();
}
