namespace App.BLL.Contracts.Properties.Commands;

public sealed class CreatePropertyCommand
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string CustomerSlug { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string AddressLine { get; init; } = default!;
    public string City { get; init; } = default!;
    public string PostalCode { get; init; } = default!;
    public Guid PropertyTypeId { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; } = true;
}
