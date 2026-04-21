namespace App.BLL.Onboarding.WorkspaceCatalog;

public class UserContextCatalogResult
{
    public string ManagementCompanyName { get; set; } = "Management Workspace";
    public bool CanManageCompanyUsers { get; set; }
    public bool HasResidentContext { get; set; }
    public IReadOnlyList<UserContextCatalogCompany> ManagementCompanies { get; set; } = [];
    public IReadOnlyList<UserContextCatalogCustomer> Customers { get; set; } = [];
}

public class UserContextCatalogCompany
{
    public Guid ManagementCompanyId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
}

public class UserContextCatalogCustomer
{
    public Guid CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
}
