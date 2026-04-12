using Base.Domain;
using App.Domain.Identity;

namespace App.Domain;

public class ResidentUser : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }

    public Guid AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    public Guid ResidentId { get; set; }
    public Resident? Resident { get; set; }
}
