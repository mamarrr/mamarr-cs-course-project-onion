namespace App.Contracts.DAL.Residents;

public sealed class ResidentContactDalDto
{
    public Guid ResidentContactId { get; init; }
    public Guid ResidentId { get; init; }
    public Guid ContactId { get; init; }
    public Guid ContactTypeId { get; init; }
    public string ContactTypeCode { get; init; } = default!;
    public string ContactTypeLabel { get; init; } = default!;
    public string ContactValue { get; init; } = default!;
    public string? Notes { get; init; }
    public DateOnly ValidFrom { get; init; }
    public DateOnly? ValidTo { get; init; }
    public bool Confirmed { get; init; }
    public bool IsPrimary { get; init; }
}
