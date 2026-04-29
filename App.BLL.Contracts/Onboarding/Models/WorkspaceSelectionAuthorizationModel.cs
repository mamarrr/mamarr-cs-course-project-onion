namespace App.BLL.Contracts.Onboarding.Models;

public sealed class WorkspaceSelectionAuthorizationModel
{
    public bool Authorized { get; init; }
    public string? NormalizedType { get; init; }
    public Guid? ManagementCompanyId { get; init; }
    public string? ManagementCompanySlug { get; init; }
    public Guid? CustomerId { get; init; }
}
