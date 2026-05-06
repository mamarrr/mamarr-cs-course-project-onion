using Base.Domain;

namespace App.DAL.DTO.Residents;

public class ResidentDalDto : BaseEntity
{
    public Guid ManagementCompanyId { get; init; }
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string IdCode { get; init; } = default!;
    public string? PreferredLanguage { get; init; }
}

public class ResidentUserContextDalDto
{
    public Guid ResidentId { get; init; }
    public string ManagementCompanySlug { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string IdCode { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
}
