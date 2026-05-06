namespace App.BLL.DTO.Residents.Commands;

public class UpdateResidentProfileCommand
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string ResidentIdCode { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string IdCode { get; init; } = default!;
    public string? PreferredLanguage { get; init; }
    
}
