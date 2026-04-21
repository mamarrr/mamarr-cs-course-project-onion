using App.BLL.ResidentWorkspace.Residents;
using App.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.ResidentWorkspace.Access;

public class ResidentAccessService : IResidentAccessService
{
    private static readonly HashSet<string> AllowedRoleCodes =
    [
        "OWNER",
        "MANAGER",
        "FINANCE",
        "SUPPORT"
    ];

    private readonly AppDbContext _dbContext;

    public ResidentAccessService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CompanyResidentsAuthorizationResult> AuthorizeAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = companySlug.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return new CompanyResidentsAuthorizationResult
            {
                CompanyNotFound = true,
                ErrorMessage = App.Resources.Views.UiText.ManagementCompanyWasNotFound
            };
        }

        var company = await _dbContext.ManagementCompanies
            .AsNoTracking()
            .Where(c => c.Slug == normalizedSlug)
            .Select(c => new { c.Id, c.Slug, c.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (company == null)
        {
            return new CompanyResidentsAuthorizationResult
            {
                CompanyNotFound = true,
                ErrorMessage = App.Resources.Views.UiText.ManagementCompanyWasNotFound
            };
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var membership = await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Include(x => x.ManagementCompanyRole)
            .FirstOrDefaultAsync(
                x => x.AppUserId == appUserId && x.ManagementCompanyId == company.Id,
                cancellationToken);

        if (membership == null ||
            !membership.IsActive ||
            membership.ValidFrom > today ||
            (membership.ValidTo.HasValue && membership.ValidTo.Value < today) ||
            !AllowedRoleCodes.Contains((membership.ManagementCompanyRole?.Code ?? string.Empty).ToUpperInvariant()))
        {
            return new CompanyResidentsAuthorizationResult
            {
                IsForbidden = true,
                ErrorMessage = App.Resources.Views.UiText.AccessDeniedDescription
            };
        }

        return new CompanyResidentsAuthorizationResult
        {
            IsAuthorized = true,
            Context = new CompanyResidentsAuthorizedContext
            {
                AppUserId = appUserId,
                ManagementCompanyId = company.Id,
                CompanySlug = company.Slug,
                CompanyName = company.Name
            }
        };
    }

    public async Task<ResidentDashboardAccessResult> ResolveDashboardAccessAsync(
        Guid appUserId,
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken = default)
    {
        var authResult = await AuthorizeAsync(appUserId, companySlug, cancellationToken);
        if (authResult.CompanyNotFound)
        {
            return new ResidentDashboardAccessResult
            {
                CompanyNotFound = true,
                ErrorMessage = authResult.ErrorMessage
            };
        }

        if (authResult.IsForbidden || authResult.Context == null)
        {
            return new ResidentDashboardAccessResult
            {
                IsForbidden = true,
                ErrorMessage = authResult.ErrorMessage
            };
        }

        var normalizedResidentIdCode = residentIdCode.Trim();
        if (string.IsNullOrWhiteSpace(normalizedResidentIdCode))
        {
            return new ResidentDashboardAccessResult
            {
                ResidentNotFound = true
            };
        }

        var resident = await _dbContext.Residents
            .AsNoTracking()
            .Where(r => r.ManagementCompanyId == authResult.Context.ManagementCompanyId &&
                        r.IdCode == normalizedResidentIdCode)
            .Select(r => new
            {
                r.Id,
                r.IdCode,
                r.FirstName,
                r.LastName,
                r.PreferredLanguage,
                r.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (resident == null)
        {
            return new ResidentDashboardAccessResult
            {
                ResidentNotFound = true
            };
        }

        var fullName = string.Join(" ", new[] { resident.FirstName, resident.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

        return new ResidentDashboardAccessResult
        {
            IsAuthorized = true,
            Context = new ResidentDashboardContext
            {
                AppUserId = authResult.Context.AppUserId,
                ManagementCompanyId = authResult.Context.ManagementCompanyId,
                CompanySlug = authResult.Context.CompanySlug,
                CompanyName = authResult.Context.CompanyName,
                ResidentId = resident.Id,
                ResidentIdCode = resident.IdCode,
                FirstName = resident.FirstName,
                LastName = resident.LastName,
                FullName = fullName,
                PreferredLanguage = resident.PreferredLanguage,
                IsActive = resident.IsActive
            }
        };
    }
}
