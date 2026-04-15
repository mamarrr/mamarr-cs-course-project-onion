using WebApp.ViewModels.Shared.Layout;

namespace WebApp.ViewModels.Management.CustomerProperties;

public class PropertyDashboardPageViewModel : IHasPageShell<PropertyPageShellViewModel>
{
    public PropertyPageShellViewModel PageShell { get; init; } = new();
    public string CompanySlug { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string CustomerSlug { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string PropertySlug { get; init; } = string.Empty;
    public string PropertyName { get; init; } = string.Empty;
    public string CurrentSection { get; init; } = string.Empty;
}

public class PropertyLayoutViewModel
{
    public string CompanySlug { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string CustomerSlug { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string PropertySlug { get; init; } = string.Empty;
    public string PropertyName { get; init; } = string.Empty;
    public string CurrentSection { get; init; } = string.Empty;
}
