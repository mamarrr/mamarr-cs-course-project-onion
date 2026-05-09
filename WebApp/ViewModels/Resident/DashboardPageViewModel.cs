using WebApp.UI.Chrome;
using WebApp.ViewModels.Dashboards;

namespace WebApp.ViewModels.Resident;

public class DashboardPageViewModel : IAppChromePage
{
    public AppChromeViewModel AppChrome { get; init; } = new();
    public string CompanySlug { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string ResidentIdCode { get; init; } = string.Empty;
    public string ResidentDisplayName { get; init; } = string.Empty;
    public string? ResidentSupportingText { get; init; }
    public string CurrentSection { get; init; } = string.Empty;
    public string CurrentSectionLabel { get; init; } = string.Empty;
    public ResidentDashboardViewModel Dashboard { get; init; } = new();
}
