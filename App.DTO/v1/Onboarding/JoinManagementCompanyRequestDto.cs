namespace App.DTO.v1.Onboarding;

public class JoinManagementCompanyRequestDto
{
    public string RegistryCode { get; set; } = default!;
    public Guid RequestedRoleId { get; set; }
    public string? Message { get; set; }
}
