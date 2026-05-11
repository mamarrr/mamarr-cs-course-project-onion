namespace App.DTO.v1.Onboarding;

public class OnboardingStatusDto
{
    public bool HasWorkspaceContext { get; set; }
    public bool CreateManagementCompany { get; set; }
    public bool JoinManagementCompany { get; set; }
    public string? DefaultPath { get; set; }
}
