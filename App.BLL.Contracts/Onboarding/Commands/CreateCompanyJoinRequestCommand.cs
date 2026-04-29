namespace App.BLL.Contracts.Onboarding.Commands;

public sealed class CreateCompanyJoinRequestCommand
{
    public Guid AppUserId { get; init; }
    public string RegistryCode { get; init; } = default!;
    public Guid RequestedRoleId { get; init; }
    public string? Message { get; init; }
}
