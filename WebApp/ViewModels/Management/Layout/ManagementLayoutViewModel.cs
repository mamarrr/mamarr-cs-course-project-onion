using App.Resources.Views;

namespace WebApp.ViewModels.Management.Layout;

public class ManagementLayoutViewModel
{
    public string CurrentController { get; init; } = string.Empty;
    public string CompanySlug { get; init; } = string.Empty;
    public string ManagementCompanyName { get; init; } = UiText.ManagementWorkspace;
    public bool CanManageCompanyUsers { get; init; }
    public bool HasResidentContext { get; init; }
    public string CurrentPathAndQuery { get; init; } = string.Empty;
    public string CurrentUiCultureName { get; init; } = string.Empty;

    public IReadOnlyCollection<ManagementLayoutContextOptionViewModel> ManagementContexts { get; init; } = [];
    public IReadOnlyCollection<ManagementLayoutContextOptionViewModel> CustomerContexts { get; init; } = [];
    public IReadOnlyCollection<ManagementLayoutCultureOptionViewModel> CultureOptions { get; init; } = [];
}

public class ManagementLayoutContextOptionViewModel
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public class ManagementLayoutCultureOptionViewModel
{
    public string Value { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public bool IsCurrent { get; init; }
}
