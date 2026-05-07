namespace App.DAL.DTO.Admin.Companies;

public class AdminCompanySearchDalDto
{
    public string? SearchText { get; set; }
    public string? Name { get; set; }
    public string? RegistryCode { get; set; }
    public string? Slug { get; set; }
}

public class AdminCompanyListItemDalDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RegistryCode { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int UsersCount { get; set; }
    public int OpenTicketsCount { get; set; }
}

public class AdminCompanyDetailsDalDto : AdminCompanyListItemDalDto
{
    public string VatNumber { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int CustomersCount { get; set; }
    public int PropertiesCount { get; set; }
    public int UnitsCount { get; set; }
    public int ResidentsCount { get; set; }
    public int TicketsCount { get; set; }
    public int VendorsCount { get; set; }
    public int PendingJoinRequestsCount { get; set; }
}

public class AdminCompanyUpdateDalDto
{
    public string Name { get; set; } = string.Empty;
    public string RegistryCode { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
