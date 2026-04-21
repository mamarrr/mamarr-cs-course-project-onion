using App.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Onboarding.WorkspaceCatalog;

public class UserWorkspaceCatalogService : IUserWorkspaceCatalogService
{
    private readonly AppDbContext _dbContext;

    public UserWorkspaceCatalogService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserWorkspaceCatalogResult> GetUserContextCatalogAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = companySlug.Trim();

        var managementContexts = await _dbContext.ManagementCompanyUsers
            .Where(x => x.AppUserId == appUserId && x.IsActive)
            .Select(x => new UserWorkspaceCatalogCompany
            {
                ManagementCompanyId = x.ManagementCompanyId,
                Slug = x.ManagementCompany!.Slug,
                CompanyName = x.ManagementCompany.Name
            })
            .ToListAsync(cancellationToken);

        var canManageCompanyUsers = await _dbContext.ManagementCompanyUsers
            .Where(x => x.AppUserId == appUserId
                        && x.IsActive
                        && x.ManagementCompany != null
                        && x.ManagementCompany.Slug == normalizedSlug
                        && x.ManagementCompanyRole != null)
            .AnyAsync(x => x.ManagementCompanyRole!.Code == "OWNER" || x.ManagementCompanyRole.Code == "MANAGER", cancellationToken);

        var selectedManagementContext = managementContexts
            .FirstOrDefault(x => string.Equals(x.Slug, normalizedSlug, StringComparison.OrdinalIgnoreCase));

        var managementCompanyName = selectedManagementContext?.CompanyName
            ?? managementContexts.Select(x => x.CompanyName).FirstOrDefault()
            ?? "Management Workspace";

        var customerContexts = await (
                from residentUser in _dbContext.ResidentUsers
                join customerRepresentative in _dbContext.CustomerRepresentatives
                    on residentUser.ResidentId equals customerRepresentative.ResidentId
                join customer in _dbContext.Customers
                    on customerRepresentative.CustomerId equals customer.Id
                where residentUser.AppUserId == appUserId
                      && residentUser.IsActive
                      && customerRepresentative.IsActive
                select new UserWorkspaceCatalogCustomer
                {
                    CustomerId = customer.Id,
                    Name = customer.Name
                })
            .Distinct()
            .ToListAsync(cancellationToken);

        var hasResidentContext = await _dbContext.ResidentUsers
            .AnyAsync(x => x.AppUserId == appUserId && x.IsActive, cancellationToken);

        return new UserWorkspaceCatalogResult
        {
            ManagementCompanyName = managementCompanyName,
            CanManageCompanyUsers = canManageCompanyUsers,
            HasResidentContext = hasResidentContext,
            ManagementCompanies = managementContexts,
            Customers = customerContexts
        };
    }
}
