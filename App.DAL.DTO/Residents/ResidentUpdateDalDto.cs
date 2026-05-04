namespace App.DAL.DTO.Residents;

public class ResidentUpdateDalDto
{
    public Guid Id { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string IdCode { get; init; } = default!;
    public string? PreferredLanguage { get; init; }
    public bool IsActive { get; init; }
}
