using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace App.Domain;

public class Lease : BaseEntity
{
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; }
    [MinLength(1)]
    public string? Notes { get; set; }

    public Guid LeaseRoleId { get; set; }
    public LeaseRole? LeaseRole { get; set; }

    public Guid UnitId { get; set; }
    public Unit? Unit { get; set; }

    public Guid ResidentId { get; set; }
    public Resident? Resident { get; set; }
}
