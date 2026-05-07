using System.ComponentModel.DataAnnotations;
using App.BLL.DTO.Admin.Lookups;
using App.Resources.Views;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WebApp.ViewModels.Admin;

namespace WebApp.ViewModels.Admin.Lookups;

public class AdminLookupListViewModel : AdminPageViewModel
{
    public AdminLookupType Type { get; set; }
    public string LookupTitle { get; set; } = string.Empty;
    [ValidateNever] public IReadOnlyList<AdminLookupTypeOptionViewModel> LookupTypes { get; set; } = [];
    [ValidateNever] public IReadOnlyList<AdminLookupItemViewModel> Items { get; set; } = [];
}

public class AdminLookupTypeOptionViewModel
{
    public AdminLookupType Type { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class AdminLookupItemViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsProtected { get; set; }
}

public class AdminLookupEditViewModel : AdminPageViewModel
{
    public AdminLookupType Type { get; set; }
    public Guid? Id { get; set; }
    public string LookupTitle { get; set; } = string.Empty;
    public bool IsProtected { get; set; }

    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.Code))]
    [Required(ErrorMessageResourceType = typeof(AdminText), ErrorMessageResourceName = nameof(AdminText.RequiredField))]
    [StringLength(80, ErrorMessageResourceType = typeof(AdminText), ErrorMessageResourceName = nameof(AdminText.StringLengthMax))]
    public string Code { get; set; } = string.Empty;

    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.Label))]
    [Required(ErrorMessageResourceType = typeof(AdminText), ErrorMessageResourceName = nameof(AdminText.RequiredField))]
    [StringLength(200, ErrorMessageResourceType = typeof(AdminText), ErrorMessageResourceName = nameof(AdminText.StringLengthMax))]
    public string Label { get; set; } = string.Empty;
}

public class AdminLookupDeleteViewModel : AdminPageViewModel
{
    public AdminLookupType Type { get; set; }
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsProtected { get; set; }
    public bool IsInUse { get; set; }
    public string? BlockReason { get; set; }
}
