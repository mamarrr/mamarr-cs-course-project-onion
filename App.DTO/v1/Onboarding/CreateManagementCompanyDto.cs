namespace App.DTO.v1.Onboarding;

public class CreateManagementCompanyDto
{
    public string Name { get; set; } = default!;
    public string RegistryCode { get; set; } = default!;
    public string? VatNumber { get; set; }
    public string Email { get; set; } = default!;
    public string? Phone { get; set; }
    public string Address { get; set; } = default!;
}
