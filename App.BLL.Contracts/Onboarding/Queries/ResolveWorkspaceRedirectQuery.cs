namespace App.BLL.Contracts.Onboarding.Queries;

public sealed class ResolveWorkspaceRedirectQuery
{
    public Guid AppUserId { get; init; }
    public WorkspaceRedirectCookieState CookieState { get; init; } = new();
}

public sealed class WorkspaceRedirectCookieState
{
    public string? ContextType { get; init; }
    public string? ManagementCompanySlug { get; init; }
    public string? CustomerId { get; init; }
}
