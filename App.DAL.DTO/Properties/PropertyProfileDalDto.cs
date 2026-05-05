namespace App.DAL.DTO.Properties;

public class PropertyProfileDalDto
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public string CustomerSlug { get; init; } = default!;
    public string CustomerName { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string AddressLine { get; init; } = default!;
    public string City { get; init; } = default!;
    public string PostalCode { get; init; } = default!;
    public string? Notes { get; init; }
    public Guid PropertyTypeId { get; init; }
    public string PropertyTypeCode { get; init; } = default!;
    public string PropertyTypeLabel { get; init; } = default!;
    
}
