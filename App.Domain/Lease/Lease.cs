using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Base.Domain;

namespace App.Domain;

public class Lease : BaseEntity
{
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; }
    [Display(ResourceType = typeof(App.Resources.Domain.Lease), Name = nameof(App.Resources.Domain.Lease.Notes))]
    [Column(TypeName = "jsonb")]
    public LangStr? Notes { get; set; }

    public Guid LeaseRoleId { get; set; }
    public LeaseRole? LeaseRole { get; set; }

    public Guid UnitId { get; set; }
    public Unit? Unit { get; set; }

    public Guid ResidentId { get; set; }
    public Resident? Resident { get; set; }
}
