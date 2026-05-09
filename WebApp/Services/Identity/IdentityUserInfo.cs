namespace WebApp.Services.Identity;

public class IdentityUserInfo
{
    public Guid Id { get; init; }
    public string Email { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public IReadOnlyList<string> Roles { get; init; } = [];
}
