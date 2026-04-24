using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.UI.Chrome;

namespace WebApp.ViewModels.Management.Users;

public class UsersPageViewModel : IAppChromePage
{
    public AppChromeViewModel AppChrome { get; init; } = new();
    public string CompanySlug { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
    public bool CurrentActorIsOwner { get; set; }
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
    public bool IsOwner { get; set; }
    public bool IsEffective { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanTransferOwnership { get; set; }
    public bool CanChangeRole { get; set; }
    public bool CanDeactivate { get; set; }
    public string? ProtectedReason { get; set; }
}

public class AddManagementUserViewModel
{
    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [EmailAddress(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.InvalidEmailAddress))]
    [Display(Name = nameof(UiText.Email), ResourceType = typeof(UiText))]
    public string Email { get; set; } = default!;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = nameof(UiText.Role), ResourceType = typeof(UiText))]
    public Guid? RoleId { get; set; }

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(255, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = nameof(UiText.JobTitle), ResourceType = typeof(UiText))]
    public string JobTitle { get; set; } = default!;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = nameof(UiText.ValidFrom), ResourceType = typeof(UiText))]
    [DataType(DataType.Date)]
    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = nameof(UiText.ValidTo), ResourceType = typeof(UiText))]
    [DataType(DataType.Date)]
    public DateOnly? ValidTo { get; set; }

    [Display(Name = nameof(UiText.Active), ResourceType = typeof(UiText))]
    public bool IsActive { get; set; } = true;
}

public class EditManagementUserViewModel : IAppChromePage
{
    public AppChromeViewModel AppChrome { get; set; } = new();
    public Guid MembershipId { get; set; }
    public string CompanySlug { get; set; } = default!;
    public string? CompanyName { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? CurrentRoleLabel { get; set; }
    public string? CurrentRoleCode { get; set; }
    public bool IsOwner { get; set; }
    public bool IsActor { get; set; }
    public bool IsEffective { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanTransferOwnership { get; set; }
    public bool CanChangeRole { get; set; }
    public bool CanDeactivate { get; set; }
    public bool OwnershipTransferRequired { get; set; }
    public string? ProtectedReason { get; set; }

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = nameof(UiText.Role), ResourceType = typeof(UiText))]
    public Guid? RoleId { get; set; }

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(255, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = nameof(UiText.JobTitle), ResourceType = typeof(UiText))]
    public string JobTitle { get; set; } = default!;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = nameof(UiText.ValidFrom), ResourceType = typeof(UiText))]
    [DataType(DataType.Date)]
    public DateOnly ValidFrom { get; set; }

    [Display(Name = nameof(UiText.ValidTo), ResourceType = typeof(UiText))]
    [DataType(DataType.Date)]
    public DateOnly? ValidTo { get; set; }

    [Display(Name = nameof(UiText.Active), ResourceType = typeof(UiText))]
    public bool IsActive { get; set; }

    public IReadOnlyList<SelectListItem> AvailableRoles { get; set; } = Array.Empty<SelectListItem>();
}

public class TransferOwnershipPageViewModel : IAppChromePage
{
    public AppChromeViewModel AppChrome { get; init; } = new();
    public string CompanySlug { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
    public string CurrentOwnerName { get; set; } = default!;
    public string CurrentOwnerEmail { get; set; } = default!;
    public IReadOnlyList<SelectListItem> Candidates { get; set; } = Array.Empty<SelectListItem>();
    public TransferOwnershipInputViewModel Transfer { get; set; } = new();
}

public class TransferOwnershipInputViewModel
{
    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = nameof(UiText.NewOwner), ResourceType = typeof(UiText))]
    public Guid? TargetMembershipId { get; set; }
}

public class PendingAccessRequestViewModel
{
    public Guid RequestId { get; set; }
    public Guid AppUserId { get; set; }
    public string RequesterName { get; set; } = default!;
    public string RequesterEmail { get; set; } = default!;
    public string RequestedRoleLabel { get; set; } = default!;
    public string RequestedRoleCode { get; set; } = default!;
    public string? Message { get; set; }
    public DateTime RequestedAt { get; set; }
}
