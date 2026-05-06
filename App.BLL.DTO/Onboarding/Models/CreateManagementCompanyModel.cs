namespace App.BLL.DTO.Onboarding.Models;

public class CreateManagementCompanyModel
{
    public Guid ManagementCompanyId { get; init; }
    public string ManagementCompanySlug { get; init; } = default!;
    public string Name { get; init; } = default!;
}
