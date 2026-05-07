using Base.Domain;

namespace App.BLL.DTO.Residents;

public class ResidentContactBllDto : BaseEntity
{
    public Guid ResidentId { get; set; }
    public Guid ContactId { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool Confirmed { get; set; }
    public bool IsPrimary { get; set; }
}
