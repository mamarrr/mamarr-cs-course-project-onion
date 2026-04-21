namespace App.BLL.Onboarding.Account;

public class OnboardingCreateManagementCompanyRequest
{
    public Guid AppUserId { get; set; }
    public string Name { get; set; } = default!;
    public string RegistryCode { get; set; } = default!;
    public string VatNumber { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string Address { get; set; } = default!;
}
