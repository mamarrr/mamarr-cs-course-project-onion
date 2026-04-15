using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WebApp.ViewModels.ManagementCustomers;

public class ManagementCustomersPageViewModel
{
    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    [ValidateNever]
    public IReadOnlyList<ManagementCustomerListItemViewModel> Customers { get; set; } = Array.Empty<ManagementCustomerListItemViewModel>();

    public AddManagementCustomerViewModel AddCustomer { get; set; } = new();
}

public class ManagementCustomerListItemViewModel
{
    public Guid CustomerId { get; set; }
    public string CustomerSlug { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string RegistryCode { get; set; } = default!;
    public string? BillingEmail { get; set; }
    public string? BillingAddress { get; set; }
    public string? Phone { get; set; }
    public IReadOnlyList<ManagementCustomerPropertyLinkViewModel> Properties { get; set; } = Array.Empty<ManagementCustomerPropertyLinkViewModel>();
}

public class ManagementCustomerPropertyLinkViewModel
{
    public string PropertySlug { get; set; } = default!;
    public string PropertyName { get; set; } = default!;
}

public class AddManagementCustomerViewModel
{
    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(200, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "Name", ResourceType = typeof(UiText))]
    public string Name { get; set; } = default!;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(50, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "RegistryCode", ResourceType = typeof(UiText))]
    public string RegistryCode { get; set; } = default!;

    [StringLength(200, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [EmailAddress(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.InvalidEmailAddress))]
    [Display(Name = "BillingEmail", ResourceType = typeof(UiText))]
    public string? BillingEmail { get; set; }

    [StringLength(255, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "BillingAddress", ResourceType = typeof(UiText))]
    public string? BillingAddress { get; set; }

    [StringLength(50, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "Phone", ResourceType = typeof(UiText))]
    public string? Phone { get; set; }
}
