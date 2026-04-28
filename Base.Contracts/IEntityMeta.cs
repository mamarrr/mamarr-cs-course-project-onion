namespace Base.Contracts;

public interface IEntityMeta
{
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    
}