namespace App.BLL.DTO.Properties.Queries;

public class GetPropertyProfileQuery
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string CustomerSlug { get; init; } = default!;
    public string PropertySlug { get; init; } = default!;
}
