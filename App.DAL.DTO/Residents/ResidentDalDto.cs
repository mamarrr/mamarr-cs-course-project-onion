using Base.Contracts;

namespace App.DAL.DTO.Residents;

public class ResidentDalDto : IBaseEntity
{
    public Guid Id { get; set; }
    public Guid ManagementCompanyId { get; init; }
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string IdCode { get; init; } = default!;
    public string? PreferredLanguage { get; init; }
    
    public DateTime CreatedAt { get; init; }
}

public class ResidentUserContextDalDto
{
    public Guid ResidentId { get; init; }
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string IdCode { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
}
