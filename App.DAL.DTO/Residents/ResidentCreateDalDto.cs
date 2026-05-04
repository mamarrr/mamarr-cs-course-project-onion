namespace App.DAL.DTO.Residents;

public class ResidentCreateDalDto
{
    public Guid ManagementCompanyId { get; init; }
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string IdCode { get; init; } = default!;
    public string? PreferredLanguage { get; init; }
}
