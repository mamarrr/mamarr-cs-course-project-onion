using WebApp.ViewModels.Customer.CustomerDashboard;
using WebApp.ViewModels.Management.Layout;
using LayoutViewModel = WebApp.ViewModels.Unit.LayoutViewModel;
using WebApp.ViewModels.Property;

namespace WebApp.ViewModels.Shared.Layout;

public interface IHasPageShell<out TPageShell>
    where TPageShell : class
{
    TPageShell PageShell { get; }
}

public class WorkspaceLayoutRequestViewModel
{
    public string CurrentController { get; init; } = string.Empty;
    public string CompanySlug { get; init; } = string.Empty;
    public string CurrentPathAndQuery { get; init; } = string.Empty;
    public string CurrentUiCultureName { get; init; } = string.Empty;
}

public class ManagementLayoutRequestViewModel : WorkspaceLayoutRequestViewModel
{
}

public class WorkspacePageShellViewModel
{
    public string Title { get; init; } = string.Empty;
    public string CurrentSectionLabel { get; init; } = string.Empty;
    public WorkspaceLayoutContextViewModel LayoutContext { get; init; } = new();
}

public class ManagementPageShellViewModel
{
    public string Title { get; init; } = string.Empty;
    public string CurrentSectionLabel { get; init; } = string.Empty;
    public ManagementLayoutViewModel Management { get; init; } = new();
}

public class CustomerPageShellViewModel : WorkspacePageShellViewModel
{
    public CustomerLayoutViewModel Customer { get; init; } = new();
}

public class PropertyPageShellViewModel : WorkspacePageShellViewModel
{
    public PropertyLayoutViewModel Property { get; init; } = new();
}

public class ResidentPageShellViewModel : WorkspacePageShellViewModel
{
    public Resident.LayoutViewModel Resident { get; init; } = new();
}

public class UnitPageShellViewModel : WorkspacePageShellViewModel
{
    public LayoutViewModel Unit { get; init; } = new();
}
