namespace App.BLL.DTO.ManagementCompanies.Models;

/// <summary>
/// Request to update a membership.
/// </summary>
public class CompanyMembershipUpdateRequest
{
    public Guid RoleId { get; set; }
    public string JobTitle { get; set; } = default!;
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}
