using Base.Contracts;

namespace Base.Domain;

public abstract class BaseEntityMeta: BaseEntity, IHasCreatedUpdatedMeta
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; } = null;
    public Guid UpdatedBy { get; set; } = default!;
}