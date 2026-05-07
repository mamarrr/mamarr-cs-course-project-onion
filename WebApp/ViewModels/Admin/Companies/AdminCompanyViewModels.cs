using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WebApp.ViewModels.Admin;

namespace WebApp.ViewModels.Admin.Companies;

public class AdminCompanySearchViewModel
{
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.SearchText))]
    public string? SearchText { get; set; }
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.Name))]
    public string? Name { get; set; }
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.RegistryCode))]
    public string? RegistryCode { get; set; }
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.Slug))]
    public string? Slug { get; set; }
}

public class AdminCompanyListViewModel : AdminPageViewModel
{
    public AdminCompanySearchViewModel Search { get; set; } = new();
    [ValidateNever] public IReadOnlyList<AdminCompanyListItemViewModel> Companies { get; set; } = [];
}

public class AdminCompanyListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RegistryCode { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int UsersCount { get; set; }
    public int OpenTicketsCount { get; set; }
}

public class AdminCompanyDetailsViewModel : AdminPageViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RegistryCode { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int UsersCount { get; set; }
    public int CustomersCount { get; set; }
    public int PropertiesCount { get; set; }
    public int UnitsCount { get; set; }
    public int ResidentsCount { get; set; }
    public int TicketsCount { get; set; }
    public int OpenTicketsCount { get; set; }
    public int VendorsCount { get; set; }
    public int PendingJoinRequestsCount { get; set; }
}

public class AdminCompanyEditViewModel : AdminPageViewModel
{
    public Guid Id { get; set; }
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.Name))]
    [Required(ErrorMessageResourceType = typeof(AdminText), ErrorMessageResourceName = nameof(AdminText.RequiredField))]
    [StringLength(200, ErrorMessageResourceType = typeof(AdminText), ErrorMessageResourceName = nameof(AdminText.StringLengthMax))]
    public string Name { get; set; } = string.Empty;
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.RegistryCode))]
    [Required(ErrorMessageResourceType = typeof(AdminText), ErrorMessageResourceName = nameof(AdminText.RequiredField))]
    [StringLength(255, ErrorMessageResourceType = typeof(AdminText), ErrorMessageResourceName = nameof(AdminText.StringLengthMax))]
    public string RegistryCode { get; set; } = string.Empty;
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.VatNumber))]
    [Required(ErrorMessageResourceType = typeof(AdminText), ErrorMessageResourceName = nameof(AdminText.RequiredField))]
    [StringLength(50, ErrorMessageResourceType = typeof(AdminText), ErrorMessageResourceName = nameof(AdminText.StringLengthMax))]
    public string VatNumber { get; set; } = string.Empty;
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.Email))]
    [Required(ErrorMessageResourceType = typeof(AdminText), ErrorMessageResourceName = nameof(AdminText.RequiredField))]
    [EmailAddress]
    [StringLength(200, ErrorMessageResourceType = typeof(AdminText), ErrorMessageResourceName = nameof(AdminText.StringLengthMax))]
    public string Email { get; set; } = string.Empty;
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.Phone))]
    [Required(ErrorMessageResourceType = typeof(AdminText), ErrorMessageResourceName = nameof(AdminText.RequiredField))]
    [StringLength(50, ErrorMessageResourceType = typeof(AdminText), ErrorMessageResourceName = nameof(AdminText.StringLengthMax))]
    public string Phone { get; set; } = string.Empty;
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.Address))]
    [Required(ErrorMessageResourceType = typeof(AdminText), ErrorMessageResourceName = nameof(AdminText.RequiredField))]
    [StringLength(300, ErrorMessageResourceType = typeof(AdminText), ErrorMessageResourceName = nameof(AdminText.StringLengthMax))]
    public string Address { get; set; } = string.Empty;
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.Slug))]
    [Required(ErrorMessageResourceType = typeof(AdminText), ErrorMessageResourceName = nameof(AdminText.RequiredField))]
    [StringLength(128, ErrorMessageResourceType = typeof(AdminText), ErrorMessageResourceName = nameof(AdminText.StringLengthMax))]
    public string Slug { get; set; } = string.Empty;
}
