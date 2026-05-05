using Base.Contracts;
using Base.Domain;

namespace App.Domain;

public class ResidentContact : BaseEntity, IHasCreatedAtMeta
{
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool Confirmed { get; set; }
    public bool IsPrimary { get; set; }
    
    public DateTime CreatedAt { get; set; }

    public Guid ResidentId { get; set; }
    public Resident? Resident { get; set; }

    public Guid ContactId { get; set; }
    public Contact? Contact { get; set; }
    
}
