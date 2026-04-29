namespace App.BLL.Contracts.Onboarding.Models;

public sealed class ApiOnboardingContextCatalogModel
{
    public IReadOnlyList<ApiOnboardingContextModel> Contexts { get; init; } = [];
    public ApiOnboardingContextModel? DefaultContext { get; init; }
}

public sealed class ApiOnboardingContextModel
{
    public string ContextType { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
    public string? CompanySlug { get; init; }
    public string? CompanyName { get; init; }
    public Guid? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string? ResidentDisplayName { get; init; }
}
