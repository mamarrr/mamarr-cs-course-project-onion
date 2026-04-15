using App.Resources.Views;

namespace WebApp.ViewModels.Onboarding;

public class ResidentAccessViewModel
{
    public string Title { get; init; } = UiText.TitleResidentAccess;
    public string Heading { get; init; } = UiText.HeadingResidentOnboarding;
    public string Description { get; init; } = UiText.ResidentOnboardingDescription;
}
