namespace App.BLL.DTO.ManagementCompanies.Models;

/// <summary>
/// Request to add a user by email.
/// </summary>
public class CompanyMembershipAddRequest
{
    public string Email { get; set; } = default!;
    public Guid RoleId { get; set; }
    public string JobTitle { get; set; } = default!;
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}
