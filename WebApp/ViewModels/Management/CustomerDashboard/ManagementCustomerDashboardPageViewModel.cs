namespace WebApp.ViewModels.Management.CustomerDashboard;

public class ManagementCustomerDashboardPageViewModel
{
    public string CompanySlug { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string CustomerSlug { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
}

public class ManagementCustomerLayoutViewModel
{
    public string CompanySlug { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string CustomerSlug { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string CurrentSection { get; init; } = string.Empty;
}
