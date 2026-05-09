using WebApp.UI.Chrome;
using WebApp.ViewModels.Dashboards;

namespace WebApp.ViewModels.Management.Dashboard;

public class DashboardPageViewModel : IAppChromePage
{
    public AppChromeViewModel AppChrome { get; init; } = new();
    public string CompanySlug { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public ManagementDashboardViewModel Dashboard { get; init; } = new();
}
