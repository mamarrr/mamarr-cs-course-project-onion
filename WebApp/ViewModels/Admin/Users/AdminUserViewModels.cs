using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WebApp.ViewModels.Admin;

namespace WebApp.ViewModels.Admin.Users;

public class AdminUserSearchViewModel
{
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.SearchText))]
    public string? SearchText { get; set; }

    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.Email))]
    public string? Email { get; set; }

    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.Name))]
    public string? Name { get; set; }

    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.LockedOnly))]
    public bool LockedOnly { get; set; }
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.HasSystemAdminRole))]
    public bool HasSystemAdminRole { get; set; }
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.CreatedFrom))]
    public DateTime? CreatedFrom { get; set; }
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.CreatedTo))]
    public DateTime? CreatedTo { get; set; }
}

public class AdminUserListViewModel : AdminPageViewModel
{
    public AdminUserSearchViewModel Search { get; set; } = new();
    [ValidateNever] public IReadOnlyList<AdminUserListItemViewModel> Users { get; set; } = [];
}

public class AdminUserListItemViewModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsLocked { get; set; }
    public bool HasSystemAdminRole { get; set; }
}

public class AdminUserDetailsViewModel : AdminPageViewModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsLocked { get; set; }
    public bool HasSystemAdminRole { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int RefreshTokenCount { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
    public IReadOnlyList<AdminUserMembershipViewModel> CompanyMemberships { get; set; } = [];
}

public class AdminUserMembershipViewModel
{
    public string CompanyName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}
