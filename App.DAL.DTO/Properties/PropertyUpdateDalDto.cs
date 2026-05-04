namespace App.DAL.DTO.Properties;

public class PropertyUpdateDalDto
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public string Name { get; init; } = default!;
    public string AddressLine { get; init; } = default!;
    public string City { get; init; } = default!;
    public string PostalCode { get; init; } = default!;
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
}
