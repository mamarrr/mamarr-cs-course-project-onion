using System.ComponentModel.DataAnnotations;
using Base.Domain;

namespace App.Domain;

public class CustomerRepresentative : BaseEntity
{
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    [MinLength(1)]
    public string? Notes { get; set; }

    public Guid CustomerRepresentativeRoleId { get; set; }
    public CustomerRepresentativeRole? CustomerRepresentativeRole { get; set; }

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid ResidentId { get; set; }
    public Resident? Resident { get; set; }
}
