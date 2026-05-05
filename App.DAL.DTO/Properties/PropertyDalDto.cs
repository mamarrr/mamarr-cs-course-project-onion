using Base.Domain;

namespace App.DAL.DTO.Properties;

public class PropertyDalDto : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Guid PropertyTypeId { get; set; }
    public string Label { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string AddressLine { get; set; } = default!;
    public string City { get; set; } = default!;
    public string PostalCode { get; set; } = default!;
    public string? Notes { get; set; }
}
