using WebApp.ViewModels.Shared.Layout;

namespace WebApp.ViewModels.Customer.CustomerDashboard;

public class CustomerDashboardPageViewModel : IHasPageShell<CustomerPageShellViewModel>
{
    public CustomerPageShellViewModel PageShell { get; init; } = new();
    public string CompanySlug { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string CustomerSlug { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
}

public class CustomerLayoutViewModel
{
    public string CompanySlug { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string CustomerSlug { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string CurrentSection { get; init; } = string.Empty;
}
