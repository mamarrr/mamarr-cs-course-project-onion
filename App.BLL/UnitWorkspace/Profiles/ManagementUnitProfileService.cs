using App.BLL.Shared.Deletion;
using App.BLL.Shared.Profiles;
using App.BLL.UnitWorkspace.Workspace;
using App.DAL.EF;
using Base.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.UnitWorkspace.Profiles;

public class ManagementUnitProfileService : IManagementUnitProfileService
{
    private readonly AppDbContext _dbContext;

    public ManagementUnitProfileService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UnitProfileModel?> GetProfileAsync(
        UnitDashboardContext context,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Units
            .AsNoTracking()
            .Where(u => u.Id == context.UnitId && u.PropertyId == context.PropertyId)
            .Select(u => new UnitProfileModel
            {
                UnitId = u.Id,
                UnitSlug = u.Slug,
                UnitNr = u.UnitNr,
                FloorNr = u.FloorNr,
                SizeM2 = u.SizeM2,
                Notes = u.Notes == null ? null : u.Notes.ToString(),
                IsActive = u.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProfileOperationResult> UpdateProfileAsync(
        UnitDashboardContext context,
        UnitProfileUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UnitNr))
        {
            return new ProfileOperationResult
            {
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.UnitNr)
            };
        }

        var unit = await _dbContext.Units
            .AsTracking()
            .FirstOrDefaultAsync(
                u => u.Id == context.UnitId && u.PropertyId == context.PropertyId,
                cancellationToken);

        if (unit == null)
        {
            return new ProfileOperationResult { NotFound = true };
        }

        var normalizedUnitNr = request.UnitNr.Trim();

        unit.UnitNr = normalizedUnitNr;
        unit.FloorNr = request.FloorNr;
        unit.SizeM2 = request.SizeM2;
        unit.IsActive = request.IsActive;

        if (string.IsNullOrWhiteSpace(request.Notes))
        {
            unit.Notes = null;
            _dbContext.Entry(unit).Property(x => x.Notes).IsModified = true;
        }
        else if (unit.Notes == null)
        {
            unit.Notes = new LangStr(request.Notes.Trim());
            _dbContext.Entry(unit).Property(x => x.Notes).IsModified = true;
        }
        else
        {
            unit.Notes.SetTranslation(request.Notes.Trim());
            _dbContext.Entry(unit).Property(x => x.Notes).IsModified = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProfileOperationResult { Success = true };
    }

    public async Task<ProfileOperationResult> DeleteProfileAsync(
        UnitDashboardContext context,
        CancellationToken cancellationToken = default)
    {
        var hasDeleteRole = await ProfileDeleteAuthorization.HasDeletePermissionAsync(
            _dbContext,
            context.ManagementCompanyId,
            context.AppUserId,
            cancellationToken);

        if (!hasDeleteRole)
        {
            return ProfileDeleteAuthorization.ForbiddenResult();
        }

        var unit = await _dbContext.Units
            .AsNoTracking()
            .Where(u => u.Id == context.UnitId && u.PropertyId == context.PropertyId)
            .Select(u => new { u.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (unit == null)
        {
            return new ProfileOperationResult { NotFound = true };
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var ticketIds = await _dbContext.Tickets
            .Where(t => t.UnitId == unit.Id && t.ManagementCompanyId == context.ManagementCompanyId)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        await ProfileDeleteOrchestrator.DeleteTicketsAsync(_dbContext, ticketIds, cancellationToken);

        await _dbContext.Leases
            .Where(l => l.UnitId == unit.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Units
            .Where(u => u.Id == unit.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new ProfileOperationResult { Success = true };
    }
}

