using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace App.Domain;

public class Unit : BaseEntity
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string UnitNr { get; set; } = default!;

    public int? FloorNr { get; set; }

    [Range(typeof(decimal), "0", "99999999.99")]
    public decimal? SizeM2 { get; set; }

    [MinLength(1)]
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }

    public ICollection<Lease>? Leases { get; set; }
    public ICollection<Ticket>? Tickets { get; set; }
}
