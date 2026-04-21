using App.BLL.ResidentWorkspace.Residents;
using App.BLL.Shared.Deletion;
using App.BLL.Shared.Profiles;
using App.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.ResidentWorkspace.Profiles;

public class ManagementResidentProfileService : IManagementResidentProfileService
{
    private readonly AppDbContext _dbContext;

    public ManagementResidentProfileService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ResidentProfileModel?> GetProfileAsync(
        ManagementResidentDashboardContext context,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Residents
            .AsNoTracking()
            .Where(r => r.Id == context.ResidentId && r.ManagementCompanyId == context.ManagementCompanyId)
            .Select(r => new ResidentProfileModel
            {
                ResidentId = r.Id,
                ResidentIdCode = r.IdCode,
                FirstName = r.FirstName,
                LastName = r.LastName,
                PreferredLanguage = r.PreferredLanguage,
                IsActive = r.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProfileOperationResult> UpdateProfileAsync(
        ManagementResidentDashboardContext context,
        ResidentProfileUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            return new ProfileOperationResult
            {
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.FirstName)
            };
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            return new ProfileOperationResult
            {
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.LastName)
            };
        }

        if (string.IsNullOrWhiteSpace(request.IdCode))
        {
            return new ProfileOperationResult
            {
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.IdCode)
            };
        }

        var normalizedFirstName = request.FirstName.Trim();
        var normalizedLastName = request.LastName.Trim();
        var normalizedIdCode = request.IdCode.Trim();
        var normalizedPreferredLanguage = string.IsNullOrWhiteSpace(request.PreferredLanguage)
            ? null
            : request.PreferredLanguage.Trim();

        var resident = await _dbContext.Residents
            .AsTracking()
            .FirstOrDefaultAsync(
                r => r.Id == context.ResidentId && r.ManagementCompanyId == context.ManagementCompanyId,
                cancellationToken);

        if (resident == null)
        {
            return new ProfileOperationResult { NotFound = true };
        }

        var duplicateIdCode = await _dbContext.Residents
            .AsNoTracking()
            .AnyAsync(
                r => r.Id != resident.Id &&
                     r.ManagementCompanyId == context.ManagementCompanyId &&
                     r.IdCode == normalizedIdCode,
                cancellationToken);

        if (duplicateIdCode)
        {
            return new ProfileOperationResult
            {
                DuplicateIdCode = true,
                ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("ResidentIdCodeAlreadyExists")
                               ?? "Resident with this ID code already exists in this company."
            };
        }

        resident.FirstName = normalizedFirstName;
        resident.LastName = normalizedLastName;
        resident.IdCode = normalizedIdCode;
        resident.PreferredLanguage = normalizedPreferredLanguage;
        resident.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProfileOperationResult { Success = true };
    }

    public async Task<ProfileOperationResult> DeleteProfileAsync(
        ManagementResidentDashboardContext context,
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

        var resident = await _dbContext.Residents
            .AsNoTracking()
            .Where(r => r.Id == context.ResidentId && r.ManagementCompanyId == context.ManagementCompanyId)
            .Select(r => new { r.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (resident == null)
        {
            return new ProfileOperationResult { NotFound = true };
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var ticketIds = await _dbContext.Tickets
            .Where(t => t.ResidentId == resident.Id && t.ManagementCompanyId == context.ManagementCompanyId)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        await ProfileDeleteOrchestrator.DeleteTicketsAsync(_dbContext, ticketIds, cancellationToken);

        await _dbContext.CustomerRepresentatives
            .Where(cr => cr.ResidentId == resident.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Leases
            .Where(l => l.ResidentId == resident.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.ResidentUsers
            .Where(ru => ru.ResidentId == resident.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var residentContactIds = await _dbContext.ResidentContacts
            .Where(rc => rc.ResidentId == resident.Id)
            .Select(rc => rc.ContactId)
            .ToListAsync(cancellationToken);

        await _dbContext.ResidentContacts
            .Where(rc => rc.ResidentId == resident.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await ProfileDeleteOrchestrator.DeleteContactsIfOrphanedAsync(
            _dbContext,
            residentContactIds,
            cancellationToken);

        await _dbContext.Residents
            .Where(r => r.Id == resident.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new ProfileOperationResult { Success = true };
    }
}

