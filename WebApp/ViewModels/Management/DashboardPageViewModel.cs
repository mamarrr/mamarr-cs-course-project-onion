using WebApp.ViewModels.Shared.Layout;

namespace WebApp.ViewModels.Management.Dashboard;

public class DashboardPageViewModel : IHasPageShell<ManagementPageShellViewModel>
{
    public ManagementPageShellViewModel PageShell { get; init; } = new();
    public string CompanySlug { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
}
