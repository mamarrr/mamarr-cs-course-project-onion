namespace App.BLL.DTO.Onboarding.Queries;

public class ResolveWorkspaceRedirectQuery
{
    public Guid AppUserId { get; init; }
    public RememberedWorkspaceContext RememberedContext { get; init; } = new();
}

public class RememberedWorkspaceContext
{
    public string? ContextType { get; init; }
    public string? ManagementCompanySlug { get; init; }
    public string? CustomerId { get; init; }
}
