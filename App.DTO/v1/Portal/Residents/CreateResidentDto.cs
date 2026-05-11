namespace App.DTO.v1.Portal.Residents;

public class CreateResidentDto
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string IdCode { get; set; } = default!;
    public string? PreferredLanguage { get; set; }
}
