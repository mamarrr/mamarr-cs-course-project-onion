namespace App.Contracts.DAL.Residents;

public sealed class ResidentListItemDalDto
{
    public Guid Id { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string IdCode { get; init; } = default!;
    public string? PreferredLanguage { get; init; }
    public bool IsActive { get; init; }
}
