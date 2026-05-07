using Base.Domain;

namespace App.DAL.DTO.Residents;

public class ResidentContactDalDto : BaseEntity
{
    public Guid ResidentId { get; init; }
    public Guid ContactId { get; init; }
    public DateOnly ValidFrom { get; init; }
    public DateOnly? ValidTo { get; init; }
    public bool Confirmed { get; init; }
    public bool IsPrimary { get; init; }
}

public class ResidentContactAssignmentDalDto : ResidentContactDalDto
{
    public Guid ContactTypeId { get; init; }
    public string ContactTypeCode { get; init; } = default!;
    public string ContactTypeLabel { get; init; } = default!;
    public string ContactValue { get; init; } = default!;
    public string? ContactNotes { get; init; }
    public DateTime CreatedAt { get; init; }
}
