namespace App.DTO.v1.Portal.Users;

public class AddCompanyUserDto
{
    public string Email { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}
