using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Residents;
using App.DAL.EF.Mappers.Residents;
using App.Domain;
using Base.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class ResidentRepository :
    BaseRepository<ResidentDalDto, Resident, AppDbContext>,
    IResidentRepository
{
    private readonly AppDbContext _dbContext;

    public ResidentRepository(AppDbContext dbContext, ResidentDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
    }

    public async Task<ResidentProfileDalDto?> FirstProfileAsync(
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanySlug = companySlug.Trim();
        var normalizedResidentIdCode = residentIdCode.Trim();

        var resident = await _dbContext.Residents
            .AsNoTracking()
            .Where(entity => entity.ManagementCompany!.Slug == normalizedCompanySlug)
            .Where(entity => entity.IdCode == normalizedResidentIdCode)
            .Select(entity => new ResidentProfileDalDto
            {
                Id = entity.Id,
                ManagementCompanyId = entity.ManagementCompanyId,
                CompanySlug = entity.ManagementCompany!.Slug,
                CompanyName = entity.ManagementCompany.Name,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                IdCode = entity.IdCode,
                PreferredLanguage = entity.PreferredLanguage,
                IsActive = entity.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        return resident;
    }

    public async Task<ResidentProfileDalDto?> FindProfileAsync(
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var resident = await _dbContext.Residents
            .AsNoTracking()
            .Where(entity => entity.Id == residentId && entity.ManagementCompanyId == managementCompanyId)
            .Select(entity => new ResidentProfileDalDto
            {
                Id = entity.Id,
                ManagementCompanyId = entity.ManagementCompanyId,
                CompanySlug = entity.ManagementCompany!.Slug,
                CompanyName = entity.ManagementCompany.Name,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                IdCode = entity.IdCode,
                PreferredLanguage = entity.PreferredLanguage,
                IsActive = entity.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        return resident;
    }

    public async Task<IReadOnlyList<ResidentListItemDalDto>> AllByCompanyAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var residents = await _dbContext.Residents
            .AsNoTracking()
            .Where(entity => entity.ManagementCompanyId == managementCompanyId)
            .OrderBy(entity => entity.LastName)
            .ThenBy(entity => entity.FirstName)
            .ThenBy(entity => entity.IdCode)
            .Select(entity => new ResidentListItemDalDto
            {
                Id = entity.Id,
                ManagementCompanyId = entity.ManagementCompanyId,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                IdCode = entity.IdCode,
                PreferredLanguage = entity.PreferredLanguage,
                IsActive = entity.IsActive
            })
            .ToListAsync(cancellationToken);

        return residents;
    }

    public async Task<ResidentUserContextDalDto?> FirstActiveUserResidentContextAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        var residentContext = await (
                from residentUser in _dbContext.ResidentUsers.AsNoTracking()
                join resident in _dbContext.Residents.AsNoTracking()
                    on residentUser.ResidentId equals resident.Id
                where residentUser.AppUserId == appUserId
                      && residentUser.IsActive
                      && resident.IsActive
                orderby resident.LastName, resident.FirstName, resident.IdCode
                select new ResidentUserContextDalDto
                {
                    ResidentId = resident.Id,
                    FirstName = resident.FirstName,
                    LastName = resident.LastName,
                    IdCode = resident.IdCode,
                    DisplayName = string.Empty
                })
            .FirstOrDefaultAsync(cancellationToken);

        if (residentContext is null)
        {
            return null;
        }

        var displayName = $"{residentContext.FirstName} {residentContext.LastName}".Trim();
        return new ResidentUserContextDalDto
        {
            ResidentId = residentContext.ResidentId,
            FirstName = residentContext.FirstName,
            LastName = residentContext.LastName,
            IdCode = residentContext.IdCode,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? residentContext.IdCode : displayName
        };
    }

    public async Task<bool> HasActiveUserResidentContextAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ResidentUsers
            .AsNoTracking()
            .AnyAsync(
                residentUser => residentUser.AppUserId == appUserId && residentUser.IsActive,
                cancellationToken);
    }

    public async Task<bool> IdCodeExistsForCompanyAsync(
        Guid managementCompanyId,
        string idCode,
        Guid? exceptResidentId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedIdCode = idCode.Trim().ToLowerInvariant();

        return await _dbContext.Residents
            .AsNoTracking()
            .Where(entity => entity.ManagementCompanyId == managementCompanyId)
            .Where(entity => exceptResidentId == null || entity.Id != exceptResidentId.Value)
            .AnyAsync(entity => entity.IdCode.ToLower() == normalizedIdCode, cancellationToken);
    }

    public Task<ResidentDalDto> AddAsync(
        ResidentCreateDalDto dto,
        CancellationToken cancellationToken = default)
    {
        var resident = new Resident
        {
            Id = Guid.NewGuid(),
            ManagementCompanyId = dto.ManagementCompanyId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IdCode = dto.IdCode,
            PreferredLanguage = dto.PreferredLanguage,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Residents.Add(resident);

        return Task.FromResult(new ResidentDalDto
        {
            Id = resident.Id,
            ManagementCompanyId = resident.ManagementCompanyId,
            FirstName = resident.FirstName,
            LastName = resident.LastName,
            IdCode = resident.IdCode,
            PreferredLanguage = resident.PreferredLanguage,
            IsActive = resident.IsActive,
            CreatedAt = resident.CreatedAt
        });
    }

    public async Task UpdateAsync(
        ResidentUpdateDalDto dto,
        CancellationToken cancellationToken = default)
    {
        var resident = await _dbContext.Residents
            .AsTracking()
            .FirstOrDefaultAsync(
                entity => entity.Id == dto.Id && entity.ManagementCompanyId == dto.ManagementCompanyId,
                cancellationToken);

        if (resident is null)
        {
            return;
        }

        resident.FirstName = dto.FirstName;
        resident.LastName = dto.LastName;
        resident.IdCode = dto.IdCode;
        resident.PreferredLanguage = dto.PreferredLanguage;
        resident.IsActive = dto.IsActive;
    }

    public async Task<bool> DeleteAsync(
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var resident = await _dbContext.Residents
            .AsNoTracking()
            .Where(entity => entity.Id == residentId && entity.ManagementCompanyId == managementCompanyId)
            .Select(entity => new { entity.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (resident is null)
        {
            return false;
        }

        var ticketIds = await _dbContext.Tickets
            .Where(ticket => ticket.ResidentId == resident.Id && ticket.ManagementCompanyId == managementCompanyId)
            .Select(ticket => ticket.Id)
            .ToListAsync(cancellationToken);

        await DeleteTicketsAsync(ticketIds, cancellationToken);

        await _dbContext.CustomerRepresentatives
            .Where(entity => entity.ResidentId == resident.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Leases
            .Where(entity => entity.ResidentId == resident.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.ResidentUsers
            .Where(entity => entity.ResidentId == resident.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var residentContactIds = await _dbContext.ResidentContacts
            .Where(entity => entity.ResidentId == resident.Id)
            .Select(entity => entity.ContactId)
            .ToListAsync(cancellationToken);

        await _dbContext.ResidentContacts
            .Where(entity => entity.ResidentId == resident.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await DeleteContactsIfOrphanedAsync(residentContactIds, cancellationToken);

        await _dbContext.Residents
            .Where(entity => entity.Id == resident.Id)
            .ExecuteDeleteAsync(cancellationToken);

        return true;
    }

    public async Task<IReadOnlyList<ResidentContactDalDto>> ContactsByResidentAsync(
        Guid residentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ResidentContacts
            .AsNoTracking()
            .Where(entity => entity.ResidentId == residentId)
            .OrderByDescending(entity => entity.IsPrimary)
            .ThenBy(entity => entity.Contact!.ContactType!.Code)
            .ThenBy(entity => entity.Contact!.ContactValue)
            .Select(entity => new ResidentContactDalDto
            {
                ResidentContactId = entity.Id,
                ResidentId = entity.ResidentId,
                ContactId = entity.ContactId,
                ContactTypeId = entity.Contact!.ContactTypeId,
                ContactTypeCode = entity.Contact.ContactType!.Code,
                ContactTypeLabel = entity.Contact.ContactType.Label.ToString(),
                ContactValue = entity.Contact.ContactValue,
                Notes = entity.Contact.Notes == null ? null : entity.Contact.Notes.ToString(),
                ValidFrom = entity.ValidFrom,
                ValidTo = entity.ValidTo,
                Confirmed = entity.Confirmed,
                IsPrimary = entity.IsPrimary
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ResidentLeaseSummaryDalDto>> LeaseSummariesByResidentAsync(
        Guid residentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Leases
            .AsNoTracking()
            .Where(entity => entity.ResidentId == residentId)
            .OrderByDescending(entity => entity.IsActive)
            .ThenByDescending(entity => entity.StartDate)
            .Select(entity => new ResidentLeaseSummaryDalDto
            {
                LeaseId = entity.Id,
                ResidentId = entity.ResidentId,
                UnitId = entity.UnitId,
                PropertyId = entity.Unit!.PropertyId,
                PropertyName = entity.Unit.Property!.Label.ToString(),
                PropertySlug = entity.Unit.Property.Slug,
                UnitNr = entity.Unit.UnitNr,
                UnitSlug = entity.Unit.Slug,
                LeaseRoleId = entity.LeaseRoleId,
                LeaseRoleCode = entity.LeaseRole!.Code,
                LeaseRoleLabel = entity.LeaseRole.Label.ToString(),
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                IsActive = entity.IsActive,
                Notes = entity.Notes == null ? null : entity.Notes.ToString()
            })
            .ToListAsync(cancellationToken);
    }

    private async Task DeleteTicketsAsync(
        IReadOnlyCollection<Guid> ticketIds,
        CancellationToken cancellationToken)
    {
        if (ticketIds.Count == 0)
        {
            return;
        }

        var scheduledWorkIds = await _dbContext.ScheduledWorks
            .Where(entity => ticketIds.Contains(entity.TicketId))
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);

        if (scheduledWorkIds.Count > 0)
        {
            await _dbContext.WorkLogs
                .Where(entity => scheduledWorkIds.Contains(entity.ScheduledWorkId))
                .ExecuteDeleteAsync(cancellationToken);

            await _dbContext.ScheduledWorks
                .Where(entity => scheduledWorkIds.Contains(entity.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await _dbContext.Tickets
            .Where(entity => ticketIds.Contains(entity.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task DeleteContactsIfOrphanedAsync(
        IReadOnlyCollection<Guid> contactIds,
        CancellationToken cancellationToken)
    {
        if (contactIds.Count == 0)
        {
            return;
        }

        var stillLinkedContactIds = await _dbContext.ResidentContacts
            .Where(entity => contactIds.Contains(entity.ContactId))
            .Select(entity => entity.ContactId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var orphanedContactIds = contactIds
            .Except(stillLinkedContactIds)
            .ToList();

        if (orphanedContactIds.Count == 0)
        {
            return;
        }

        await _dbContext.Contacts
            .Where(entity => orphanedContactIds.Contains(entity.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
