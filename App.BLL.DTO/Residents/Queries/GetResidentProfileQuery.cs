namespace App.BLL.DTO.Residents.Queries;

public class GetResidentProfileQuery
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string ResidentIdCode { get; init; } = default!;
}
