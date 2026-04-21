using WebApp.ViewModels.Shared.Layout;

namespace WebApp.ViewModels.Resident;

public class DashboardPageViewModel : IHasPageShell<ResidentPageShellViewModel>
{
    public ResidentPageShellViewModel PageShell { get; init; } = new();
    public string CompanySlug { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string ResidentIdCode { get; init; } = string.Empty;
    public string ResidentDisplayName { get; init; } = string.Empty;
    public string? ResidentSupportingText { get; init; }
    public string CurrentSection { get; init; } = string.Empty;
}
