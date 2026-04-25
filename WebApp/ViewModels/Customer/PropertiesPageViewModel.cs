using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WebApp.UI.Chrome;

namespace WebApp.ViewModels.Customer.CustomerProperties;

public class PropertiesPageViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = string.Empty;

    [ValidateNever]
    public string CompanyName { get; set; } = string.Empty;

    [ValidateNever]
    public string CustomerSlug { get; set; } = string.Empty;

    [ValidateNever]
    public string CustomerName { get; set; } = string.Empty;

    [ValidateNever]
    public IReadOnlyList<CustomerPropertyListItemViewModel> Properties { get; set; } = Array.Empty<CustomerPropertyListItemViewModel>();

    [ValidateNever]
    public IReadOnlyList<PropertyTypeOptionViewModel> PropertyTypeOptions { get; set; } = Array.Empty<PropertyTypeOptionViewModel>();

    public AddPropertyViewModel AddProperty { get; set; } = new();
}

public class CustomerPropertyListItemViewModel
{
    public Guid PropertyId { get; set; }
    public string PropertySlug { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string PropertyTypeLabel { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class AddPropertyViewModel
{
    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(200, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "Name", ResourceType = typeof(UiText))]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(255, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "AddressLine", ResourceType = typeof(UiText))]
    public string AddressLine { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(120, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "City", ResourceType = typeof(UiText))]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(20, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "PostalCode", ResourceType = typeof(UiText))]
    public string PostalCode { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = "PropertyType", ResourceType = typeof(UiText))]
    public Guid? PropertyTypeId { get; set; }

    [StringLength(2000, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "Notes", ResourceType = typeof(UiText))]
    public string? Notes { get; set; }

    [Display(Name = "Active", ResourceType = typeof(UiText))]
    public bool IsActive { get; set; } = true;
}

public class PropertyTypeOptionViewModel
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

