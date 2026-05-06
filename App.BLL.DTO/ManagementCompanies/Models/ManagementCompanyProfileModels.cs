namespace App.BLL.DTO.ManagementCompanies.Models;

public class CompanyProfileModel
{
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RegistryCode { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    
}

public class CompanyProfileUpdateRequest
{
    public string Name { get; set; } = string.Empty;
    public string RegistryCode { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    
}

