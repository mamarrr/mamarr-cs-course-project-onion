namespace App.BLL.DTO.Residents.Models;

public class ResidentContactModel
{
    public Guid ResidentContactId { get; init; }
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
