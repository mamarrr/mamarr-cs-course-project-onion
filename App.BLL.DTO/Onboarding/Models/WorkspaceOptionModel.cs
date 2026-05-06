namespace App.BLL.DTO.Onboarding.Models;

public class WorkspaceOptionModel
{
    public Guid Id { get; init; }
    public string ContextType { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Slug { get; init; }
    public string? ManagementCompanySlug { get; init; }
    public bool IsDefault { get; init; }
}
