using Base.Domain;

namespace App.BLL.DTO.Leases;

public class LeaseBllDto : BaseEntity
{
    public Guid UnitId { get; set; }
    public Guid ResidentId { get; set; }
    public Guid LeaseRoleId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Notes { get; set; }
}

