using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.Shared.Routing;
using App.BLL.UnitWorkspace.Access;
using App.BLL.UnitWorkspace.Units;
using App.DAL.EF;
using App.Domain;
using Base.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.UnitWorkspace.Workspace;

public class ManagementPropertyUnitService :
    IManagementPropertyUnitService,
    IManagementUnitDashboardService
{
    private const int MinFloorNr = -200;
    private const int MaxFloorNr = 300;
    private const decimal MinSizeM2 = 0m;
    private const decimal MaxSizeM2 = 99999999.99m;

    private readonly AppDbContext _dbContext;

    public ManagementPropertyUnitService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ManagementPropertyUnitListResult> ListUnitsAsync(
        PropertyDashboardContext context,
        CancellationToken cancellationToken = default)
    {
        var units = await _dbContext.Units
            .AsNoTracking()
            .Where(u => u.PropertyId == context.PropertyId)
            .OrderBy(u => u.UnitNr)
            .ThenBy(u => u.FloorNr)
            .ThenBy(u => u.Id)
            .Select(u => new ManagementPropertyUnitListItem
            {
                UnitId = u.Id,
                UnitSlug = u.Slug,
                UnitNr = u.UnitNr,
                FloorNr = u.FloorNr,
                SizeM2 = u.SizeM2
            })
            .ToListAsync(cancellationToken);

        return new ManagementPropertyUnitListResult
        {
            Units = units
        };
    }

    public async Task<ManagementPropertyUnitCreateResult> CreateUnitAsync(
        PropertyDashboardContext context,
        ManagementPropertyUnitCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedUnitNr = request.UnitNr?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedUnitNr))
        {
            return new ManagementPropertyUnitCreateResult
            {
                InvalidUnitNr = true,
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace(
                    "{0}",
                    App.Resources.Views.UiText.ResourceManager.GetString("UnitNr") ?? "Unit number")
            };
        }

        if (request.FloorNr.HasValue &&
            (request.FloorNr.Value < MinFloorNr || request.FloorNr.Value > MaxFloorNr))
        {
            return new ManagementPropertyUnitCreateResult
            {
                InvalidFloorNr = true,
                ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("InvalidData")
                               ?? $"Floor number must be between {MinFloorNr} and {MaxFloorNr}."
            };
        }

        if (request.SizeM2.HasValue &&
            (request.SizeM2.Value < MinSizeM2 || request.SizeM2.Value > MaxSizeM2))
        {
            return new ManagementPropertyUnitCreateResult
            {
                InvalidSizeM2 = true,
                ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("InvalidData")
                               ?? $"Size must be between {MinSizeM2} and {MaxSizeM2}."
            };
        }

        var baseSlug = SlugGenerator.GenerateSlug(normalizedUnitNr);
        var existingSlugs = await _dbContext.Units
            .AsNoTracking()
            .Where(u => u.PropertyId == context.PropertyId && u.Slug.StartsWith(baseSlug))
            .Select(u => u.Slug)
            .ToListAsync(cancellationToken);
        var uniqueSlug = SlugGenerator.EnsureUniqueSlug(baseSlug, existingSlugs);

        var normalizedNotes = string.IsNullOrWhiteSpace(request.Notes)
            ? null
            : request.Notes.Trim();

        var unit = new Unit
        {
            Id = Guid.NewGuid(),
            UnitNr = normalizedUnitNr,
            Slug = uniqueSlug,
            FloorNr = request.FloorNr,
            SizeM2 = request.SizeM2,
            Notes = normalizedNotes == null ? null : new LangStr(normalizedNotes),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            PropertyId = context.PropertyId
        };

        _dbContext.Units.Add(unit);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ManagementPropertyUnitCreateResult
        {
            Success = true,
            CreatedUnitId = unit.Id,
            CreatedUnitSlug = unit.Slug
        };
    }

    public async Task<ManagementUnitDashboardAccessResult> ResolveUnitDashboardContextAsync(
        PropertyDashboardContext context,
        string unitSlug,
        CancellationToken cancellationToken = default)
    {
        var normalizedUnitSlug = unitSlug.Trim();
        if (string.IsNullOrWhiteSpace(normalizedUnitSlug))
        {
            return new ManagementUnitDashboardAccessResult
            {
                UnitNotFound = true
            };
        }

        var unit = await _dbContext.Units
            .AsNoTracking()
            .Where(u => u.PropertyId == context.PropertyId && u.Slug == normalizedUnitSlug)
            .Select(u => new
            {
                u.Id,
                u.Slug,
                u.UnitNr
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (unit == null)
        {
            return new ManagementUnitDashboardAccessResult
            {
                UnitNotFound = true
            };
        }

        return new ManagementUnitDashboardAccessResult
        {
            IsAuthorized = true,
            Context = new ManagementUnitDashboardContext
            {
                AppUserId = context.AppUserId,
                ManagementCompanyId = context.ManagementCompanyId,
                CompanySlug = context.CompanySlug,
                CompanyName = context.CompanyName,
                CustomerId = context.CustomerId,
                CustomerSlug = context.CustomerSlug,
                CustomerName = context.CustomerName,
                PropertyId = context.PropertyId,
                PropertySlug = context.PropertySlug,
                PropertyName = context.PropertyName,
                UnitId = unit.Id,
                UnitSlug = unit.Slug,
                UnitNr = unit.UnitNr
            }
        };
    }
}
