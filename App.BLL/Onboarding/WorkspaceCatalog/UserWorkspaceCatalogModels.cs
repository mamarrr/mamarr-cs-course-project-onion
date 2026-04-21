namespace App.BLL.Onboarding.WorkspaceCatalog;

public class UserWorkspaceCatalogResult
{
    public string ManagementCompanyName { get; set; } = "Management Workspace";
    public bool CanManageCompanyUsers { get; set; }
    public bool HasResidentContext { get; set; }
    public IReadOnlyList<UserWorkspaceCatalogCompany> ManagementCompanies { get; set; } = [];
    public IReadOnlyList<UserWorkspaceCatalogCustomer> Customers { get; set; } = [];
}

public class UserWorkspaceCatalogCompany
{
    public Guid ManagementCompanyId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
}

public class UserWorkspaceCatalogCustomer
{
    public Guid CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
}
