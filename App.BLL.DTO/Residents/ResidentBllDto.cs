using Base.Domain;

namespace App.BLL.DTO.Residents;

public class ResidentBllDto : BaseEntity
{
    public Guid ManagementCompanyId { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string IdCode { get; set; } = default!;
    public string? PreferredLanguage { get; set; }
}

