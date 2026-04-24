using WebApp.UI.Chrome;

namespace WebApp.ViewModels.Property;

public class DashboardPageViewModel : IAppChromePage
{
    public AppChromeViewModel AppChrome { get; init; } = new();
    public string CompanySlug { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string CustomerSlug { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string PropertySlug { get; init; } = string.Empty;
    public string PropertyName { get; init; } = string.Empty;
    public string CurrentSection { get; init; } = string.Empty;
}
