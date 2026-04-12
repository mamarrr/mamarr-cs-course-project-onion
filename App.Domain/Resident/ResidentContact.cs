using Base.Domain;

namespace App.Domain;

public class ResidentContact : BaseEntity
{
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool Confirmed { get; set; }
    public bool IsPrimary { get; set; }

    public Guid ResidentId { get; set; }
    public Resident? Resident { get; set; }

    public Guid ContactId { get; set; }
    public Contact? Contact { get; set; }
}
