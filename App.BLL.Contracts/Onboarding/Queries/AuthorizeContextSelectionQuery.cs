namespace App.BLL.Contracts.Onboarding.Queries;

public class AuthorizeContextSelectionQuery
{
    public Guid AppUserId { get; init; }
    public string ContextType { get; init; } = default!;
    public Guid? ContextId { get; init; }
}
