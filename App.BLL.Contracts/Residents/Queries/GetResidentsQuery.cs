namespace App.BLL.Contracts.Residents.Queries;

public sealed class GetResidentsQuery
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
}
