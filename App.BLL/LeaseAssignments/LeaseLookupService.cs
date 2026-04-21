using App.BLL.ResidentWorkspace.Residents;
using App.BLL.UnitWorkspace.Workspace;
using App.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.LeaseAssignments;

public class LeaseLookupService : ILeaseLookupService
{
    private const int MaxResults = 20;

    private readonly AppDbContext _dbContext;

    public LeaseLookupService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LeasePropertySearchResult> SearchPropertiesAsync(
        ResidentDashboardContext context,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = searchTerm?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return new LeasePropertySearchResult();
        }

        var candidates = await _dbContext.Properties
            .AsNoTracking()
            .Where(p => p.Customer!.ManagementCompanyId == context.ManagementCompanyId)
            .OrderBy(p => p.Slug)
            .ThenBy(p => p.AddressLine)
            .Take(250)
            .Select(p => new
            {
                PropertyId = p.Id,
                CustomerId = p.CustomerId,
                PropertySlug = p.Slug,
                Label = p.Label,
                CustomerSlug = p.Customer!.Slug,
                CustomerName = p.Customer.Name,
                AddressLine = p.AddressLine,
                City = p.City,
                PostalCode = p.PostalCode
            })
            .ToListAsync(cancellationToken);

        static bool ContainsCI(string? value, string term)
            => !string.IsNullOrWhiteSpace(value) && value.Contains(term, StringComparison.OrdinalIgnoreCase);

        var properties = candidates
            .Where(p =>
                ContainsCI(p.Label.ToString(), normalizedSearch) ||
                ContainsCI(p.AddressLine, normalizedSearch) ||
                ContainsCI(p.City, normalizedSearch) ||
                ContainsCI(p.PostalCode, normalizedSearch) ||
                ContainsCI(p.CustomerName, normalizedSearch) ||
                ContainsCI(p.PropertySlug, normalizedSearch))
            .Take(MaxResults)
            .Select(p => new LeasePropertySearchItem
            {
                PropertyId = p.PropertyId,
                CustomerId = p.CustomerId,
                PropertySlug = p.PropertySlug,
                PropertyName = p.Label.ToString(),
                CustomerSlug = p.CustomerSlug,
                CustomerName = p.CustomerName,
                AddressLine = p.AddressLine,
                City = p.City,
                PostalCode = p.PostalCode
            })
            .ToList();

        return new LeasePropertySearchResult
        {
            Properties = properties
        };
    }

    public async Task<LeaseUnitOptionsResult> ListUnitsForPropertyAsync(
        ResidentDashboardContext context,
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        var property = await _dbContext.Properties
            .AsNoTracking()
            .Where(p => p.Id == propertyId && p.Customer!.ManagementCompanyId == context.ManagementCompanyId)
            .Select(p => new { p.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
        {
            return new LeaseUnitOptionsResult
            {
                PropertyNotFound = true,
                
                ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("PropertyWasNotFound") ?? "Property was not found."
            };
        }

        var units = await _dbContext.Units
            .AsNoTracking()
            .Where(u => u.PropertyId == property.Id)
            .OrderBy(u => u.UnitNr)
            .ThenBy(u => u.FloorNr)
            .ThenBy(u => u.Id)
            .Select(u => new LeaseUnitOption
            {
                UnitId = u.Id,
                UnitSlug = u.Slug,
                UnitNr = u.UnitNr,
                FloorNr = u.FloorNr,
                IsActive = u.IsActive
            })
            .ToListAsync(cancellationToken);

        return new LeaseUnitOptionsResult
        {
            Success = true,
            Units = units
        };
    }

    public async Task<LeaseResidentSearchResult> SearchResidentsAsync(
        UnitDashboardContext context,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = searchTerm?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return new LeaseResidentSearchResult();
        }

        var pattern = $"%{normalizedSearch}%";

        var residents = await _dbContext.Residents
            .AsNoTracking()
            .Where(r => r.ManagementCompanyId == context.ManagementCompanyId)
            .Where(r =>
                EF.Functions.ILike(r.FirstName, pattern) ||
                EF.Functions.ILike(r.LastName, pattern) ||
                EF.Functions.ILike((r.FirstName + " " + r.LastName), pattern) ||
                EF.Functions.ILike(r.IdCode, pattern))
            .OrderBy(r => r.FirstName)
            .ThenBy(r => r.LastName)
            .ThenBy(r => r.IdCode)
            .Take(MaxResults)
            .Select(r => new LeaseResidentSearchItem
            {
                ResidentId = r.Id,
                FullName = string.Join(" ", new[] { r.FirstName, r.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))),
                IdCode = r.IdCode,
                IsActive = r.IsActive
            })
            .ToListAsync(cancellationToken);

        return new LeaseResidentSearchResult
        {
            Residents = residents
        };
    }

    public async Task<LeaseRoleOptionsResult> ListLeaseRolesAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await _dbContext.LeaseRoles
            .AsNoTracking()
            .OrderBy(r => r.Label)
            .ThenBy(r => r.Code)
            .Select(r => new LeaseRoleOption
            {
                LeaseRoleId = r.Id,
                Code = r.Code,
                Label = r.Label.ToString()
            })
            .ToListAsync(cancellationToken);

        return new LeaseRoleOptionsResult
        {
            Roles = roles
        };
    }
}
