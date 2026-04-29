namespace App.BLL.Contracts.Residents.Models;

public sealed class ResidentListItemModel
{
    public Guid ResidentId { get; init; }
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string FullName { get; init; } = default!;
    public string IdCode { get; init; } = default!;
    public string? PreferredLanguage { get; init; }
    public bool IsActive { get; init; }
}
