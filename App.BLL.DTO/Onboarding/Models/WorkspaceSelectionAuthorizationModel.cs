namespace App.BLL.DTO.Onboarding.Models;

public class WorkspaceSelectionAuthorizationModel
{
    public bool Authorized { get; init; }
    public string? NormalizedType { get; init; }
    public Guid? ManagementCompanyId { get; init; }
    public string? ManagementCompanySlug { get; init; }
    public Guid? CustomerId { get; init; }
}
