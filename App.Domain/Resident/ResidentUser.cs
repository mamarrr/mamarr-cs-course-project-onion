using Base.Domain;
using App.Domain.Identity;
using Base.Contracts;

namespace App.Domain;

public class ResidentUser : BaseEntity, IHasCreatedAtMeta
{
    public DateTime CreatedAt { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }

    public Guid AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    public Guid ResidentId { get; set; }
    public Resident? Resident { get; set; }
}
