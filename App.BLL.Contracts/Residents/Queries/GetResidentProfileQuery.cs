namespace App.BLL.Contracts.Residents.Queries;

public sealed class GetResidentProfileQuery
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string ResidentIdCode { get; init; } = default!;
}
