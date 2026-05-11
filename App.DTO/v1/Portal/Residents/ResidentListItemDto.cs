namespace App.DTO.v1.Portal.Residents;

public class ResidentListItemDto
{
    public Guid ResidentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string IdCode { get; set; } = string.Empty;
    public string? PreferredLanguage { get; set; }
    public string Path { get; set; } = string.Empty;
}
