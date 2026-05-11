namespace App.DTO.v1.Portal.Users;

public class CompanyUserRoleOptionDto
{
    public Guid RoleId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
}
