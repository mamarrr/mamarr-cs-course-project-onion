namespace App.DTO.v1.Portal.Properties;

public class CreatePropertyDto
{
    public string Name { get; set; } = string.Empty;
    public Guid PropertyTypeId { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
