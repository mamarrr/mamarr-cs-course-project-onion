namespace App.DAL.DTO.Properties;

public class PropertyCreateDalDto
{
    public Guid CustomerId { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string AddressLine { get; init; } = default!;
    public string City { get; init; } = default!;
    public string PostalCode { get; init; } = default!;
    public Guid PropertyTypeId { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; } = true;
}
