using Base.Contracts;

namespace Base.Domain;

public abstract class BaseEntityWithCreatedAtMeta: BaseEntity, IHasCreatedAtMeta
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}