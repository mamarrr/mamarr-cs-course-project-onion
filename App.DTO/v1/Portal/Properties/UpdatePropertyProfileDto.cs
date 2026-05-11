namespace App.DTO.v1.Portal.Properties;

public class UpdatePropertyProfileDto
{
    public string Name { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
