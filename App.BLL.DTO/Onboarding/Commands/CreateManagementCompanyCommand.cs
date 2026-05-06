namespace App.BLL.DTO.Onboarding.Commands;

public class CreateManagementCompanyCommand
{
    public Guid AppUserId { get; init; }
    public string Name { get; init; } = default!;
    public string RegistryCode { get; init; } = default!;
    public string VatNumber { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string Phone { get; init; } = default!;
    public string Address { get; init; } = default!;
}
