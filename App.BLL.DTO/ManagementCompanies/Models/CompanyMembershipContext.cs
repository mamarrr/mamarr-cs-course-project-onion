namespace App.BLL.DTO.ManagementCompanies.Models;

/// <summary>
/// Effective management membership context.
/// </summary>
public class CompanyMembershipContext
{
    public Guid AppUserId { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
    public Guid ActorMembershipId { get; set; }
    public Guid ActorRoleId { get; set; }
    public string ActorRoleCode { get; set; } = default!;
    public string ActorRoleLabel { get; set; } = default!;
    public bool IsOwner { get; set; }
    public bool IsAdmin { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}
