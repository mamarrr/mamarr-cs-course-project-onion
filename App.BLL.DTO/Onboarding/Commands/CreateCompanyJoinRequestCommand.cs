namespace App.BLL.DTO.Onboarding.Commands;

public class CreateCompanyJoinRequestCommand
{
    public Guid AppUserId { get; init; }
    public string RegistryCode { get; init; } = default!;
    public Guid RequestedRoleId { get; init; }
    public string? Message { get; init; }
}
