namespace App.BLL.DTO.Admin.Companies;

public class AdminCompanySearchDto
{
    public string? SearchText { get; set; }
    public string? Name { get; set; }
    public string? RegistryCode { get; set; }
    public string? Slug { get; set; }
}

public class AdminCompanyListDto
{
    public AdminCompanySearchDto Search { get; set; } = new();
    public IReadOnlyList<AdminCompanyListItemDto> Companies { get; set; } = [];
}

public class AdminCompanyListItemDto
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

public class AdminCompanyDetailsDto : AdminCompanyListItemDto
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

public class AdminCompanyEditDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RegistryCode { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class AdminCompanyUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string RegistryCode { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
