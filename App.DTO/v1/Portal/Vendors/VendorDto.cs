namespace App.DTO.v1.Portal.Vendors;

public class VendorDto
{
    public Guid VendorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RegistryCode { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
}
