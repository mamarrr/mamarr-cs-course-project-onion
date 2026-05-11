namespace App.DTO.v1.Onboarding;

public class CreatedManagementCompanyDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Path { get; set; } = default!;
}
