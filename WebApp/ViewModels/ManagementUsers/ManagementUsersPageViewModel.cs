using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApp.ViewModels.ManagementUsers;

public class ManagementUsersPageViewModel
{
    public string CompanySlug { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
    public IReadOnlyList<ManagementUserListItemViewModel> Members { get; set; } = Array.Empty<ManagementUserListItemViewModel>();
    public AddManagementUserViewModel AddUser { get; set; } = new();
    public IReadOnlyList<PendingAccessRequestViewModel> PendingRequests { get; set; } = Array.Empty<PendingAccessRequestViewModel>();
    public IReadOnlyList<SelectListItem> AvailableRoles { get; set; } = Array.Empty<SelectListItem>();
}

public class ManagementUserListItemViewModel
{
    public Guid MembershipId { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string RoleLabel { get; set; } = default!;
    public string RoleCode { get; set; } = default!;
    public string JobTitle { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsActor { get; set; }
}

public class AddManagementUserViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = default!;

    [Required]
    [Display(Name = "Role")]
    public Guid? RoleId { get; set; }

    [Required]
    [StringLength(255, MinimumLength = 1)]
    [Display(Name = "Job title")]
    public string JobTitle { get; set; } = default!;

    [Required]
    [Display(Name = "Valid from")]
    [DataType(DataType.Date)]
    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Valid to")]
    [DataType(DataType.Date)]
    public DateOnly? ValidTo { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

public class EditManagementUserViewModel
{
    public Guid MembershipId { get; set; }
    public string CompanySlug { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;

    [Required]
    [Display(Name = "Role")]
    public Guid? RoleId { get; set; }

    [Required]
    [StringLength(255, MinimumLength = 1)]
    [Display(Name = "Job title")]
    public string JobTitle { get; set; } = default!;

    [Required]
    [Display(Name = "Valid from")]
    [DataType(DataType.Date)]
    public DateOnly ValidFrom { get; set; }

    [Display(Name = "Valid to")]
    [DataType(DataType.Date)]
    public DateOnly? ValidTo { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; }

    public IReadOnlyList<SelectListItem> AvailableRoles { get; set; } = Array.Empty<SelectListItem>();
}

public class PendingAccessRequestViewModel
{
    public Guid RequestId { get; set; }
    public string RequesterName { get; set; } = default!;
    public string RequesterEmail { get; set; } = default!;
    public string RequestedRoleLabel { get; set; } = default!;
    public string RequestedRoleCode { get; set; } = default!;
    public DateTime RequestedAt { get; set; }
}
