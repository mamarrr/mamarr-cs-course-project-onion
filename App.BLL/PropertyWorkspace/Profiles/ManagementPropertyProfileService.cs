using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.Shared.Deletion;
using App.BLL.Shared.Profiles;
using App.DAL.EF;
using Base.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.PropertyWorkspace.Profiles;

public class ManagementPropertyProfileService : IManagementPropertyProfileService
{
    private readonly AppDbContext _dbContext;

    public ManagementPropertyProfileService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PropertyProfileModel?> GetProfileAsync(
        ManagementCustomerPropertyDashboardContext context,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Properties
            .AsNoTracking()
            .Where(p => p.Id == context.PropertyId && p.CustomerId == context.CustomerId)
            .Select(p => new PropertyProfileModel
            {
                PropertyId = p.Id,
                PropertySlug = p.Slug,
                Name = p.Label.ToString(),
                AddressLine = p.AddressLine,
                City = p.City,
                PostalCode = p.PostalCode,
                Notes = p.Notes == null ? null : p.Notes.ToString(),
                IsActive = p.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProfileOperationResult> UpdateProfileAsync(
        ManagementCustomerPropertyDashboardContext context,
        PropertyProfileUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new ProfileOperationResult
            {
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.Name)
            };
        }

        if (string.IsNullOrWhiteSpace(request.AddressLine))
        {
            return new ProfileOperationResult
            {
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.AddressLine)
            };
        }

        if (string.IsNullOrWhiteSpace(request.City))
        {
            return new ProfileOperationResult
            {
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.City)
            };
        }

        if (string.IsNullOrWhiteSpace(request.PostalCode))
        {
            return new ProfileOperationResult
            {
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.PostalCode)
            };
        }

        var property = await _dbContext.Properties
            .AsTracking()
            .FirstOrDefaultAsync(
                p => p.Id == context.PropertyId && p.CustomerId == context.CustomerId,
                cancellationToken);

        if (property == null)
        {
            return new ProfileOperationResult { NotFound = true };
        }

        property.Label.SetTranslation(request.Name.Trim());
        _dbContext.Entry(property).Property(x => x.Label).IsModified = true;

        property.AddressLine = request.AddressLine.Trim();
        property.City = request.City.Trim();
        property.PostalCode = request.PostalCode.Trim();
        property.IsActive = request.IsActive;

        if (string.IsNullOrWhiteSpace(request.Notes))
        {
            property.Notes = null;
        }
        else if (property.Notes == null)
        {
            property.Notes = new LangStr(request.Notes.Trim());
            _dbContext.Entry(property).Property(x => x.Notes).IsModified = true;
        }
        else
        {
            property.Notes.SetTranslation(request.Notes.Trim());
            _dbContext.Entry(property).Property(x => x.Notes).IsModified = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProfileOperationResult { Success = true };
    }

    public async Task<ProfileOperationResult> DeleteProfileAsync(
        ManagementCustomerPropertyDashboardContext context,
        CancellationToken cancellationToken = default)
    {
        var hasDeleteRole = await ManagementProfileDeleteAuthorization.HasDeletePermissionAsync(
            _dbContext,
            context.ManagementCompanyId,
            context.AppUserId,
            cancellationToken);

        if (!hasDeleteRole)
        {
            return ManagementProfileDeleteAuthorization.ForbiddenResult();
        }

        var property = await _dbContext.Properties
            .AsNoTracking()
            .Where(p => p.Id == context.PropertyId && p.CustomerId == context.CustomerId)
            .Select(p => new { p.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
        {
            return new ProfileOperationResult { NotFound = true };
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var unitIds = await _dbContext.Units
            .Where(u => u.PropertyId == property.Id)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var ticketIds = await _dbContext.Tickets
            .Where(t => (t.PropertyId.HasValue && t.PropertyId.Value == property.Id)
                        || (t.UnitId.HasValue && unitIds.Contains(t.UnitId.Value)))
            .Where(t => t.ManagementCompanyId == context.ManagementCompanyId)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        await ManagementProfileDeleteOrchestrator.DeleteTicketsAsync(_dbContext, ticketIds, cancellationToken);

        await _dbContext.Leases
            .Where(l => unitIds.Contains(l.UnitId))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Units
            .Where(u => unitIds.Contains(u.Id))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Properties
            .Where(p => p.Id == property.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new ProfileOperationResult { Success = true };
    }
}

