namespace App.BLL.Onboarding.ContextSelection;

public sealed class WorkspaceRedirectCookieState
{
    public string? ContextType { get; init; }
    public string? ManagementCompanySlug { get; init; }
    public string? CustomerId { get; init; }
}

public enum WorkspaceRedirectDestination
{
    Home,
    ManagementDashboard,
    CustomerDashboard,
    ResidentDashboard
}

public sealed class WorkspaceRedirectTarget
{
    public required WorkspaceRedirectDestination Destination { get; init; }
    public string? CompanySlug { get; init; }
}

public sealed class WorkspaceRedirectAuthorizationResult
{
    public bool Authorized { get; init; }
    public string? NormalizedType { get; init; }
    public Guid? ManagementCompanyId { get; init; }
    public string? ManagementCompanySlug { get; init; }
    public Guid? CustomerId { get; init; }
}
