namespace App.BLL.DTO.ManagementCompanies.Models;

public class CompanyMembershipRoleOption
{
    public Guid RoleId { get; set; }
    public string RoleCode { get; set; } = default!;
    public string RoleLabel { get; set; } = default!;
}
