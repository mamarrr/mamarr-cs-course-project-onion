using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Leases;
using App.DAL.DTO.Residents;
using App.DAL.DTO.Tickets;
using App.DAL.EF.Mappers.Residents;
using App.Domain;
using Base.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class ResidentRepository :
    BaseRepository<ResidentDalDto, Resident, AppDbContext>,
    IResidentRepository
{
    private const int MaxLeaseAssignmentSearchResults = 20;

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
            })
            .ToListAsync(cancellationToken);

        return residents;
    }

    public async Task<ResidentUserContextDalDto?> FirstActiveUserResidentContextAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var residentContext = await (
                from residentUser in _dbContext.ResidentUsers.AsNoTracking()
                join resident in _dbContext.Residents.AsNoTracking()
                    on residentUser.ResidentId equals resident.Id
                where residentUser.AppUserId == appUserId
                      && residentUser.ValidFrom <= today
                      && (!residentUser.ValidTo.HasValue || residentUser.ValidTo.Value >= today)
                orderby resident.LastName, resident.FirstName, resident.IdCode
                select new ResidentUserContextDalDto
                {
                    ResidentId = resident.Id,
                    ManagementCompanySlug = resident.ManagementCompany!.Slug,
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
            ManagementCompanySlug = residentContext.ManagementCompanySlug,
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _dbContext.ResidentUsers
            .AsNoTracking()
            .AnyAsync(
                residentUser => residentUser.AppUserId == appUserId
                                && residentUser.ValidFrom <= today
                                && (!residentUser.ValidTo.HasValue
                                    || residentUser.ValidTo.Value >= today),
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

    public Task<bool> ExistsInCompanyAsync(
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Residents
            .AsNoTracking()
            .AnyAsync(
                entity => entity.Id == residentId && entity.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }

    public Task<bool> IsLinkedToUnitAsync(
        Guid residentId,
        Guid unitId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return _dbContext.Leases
            .AsNoTracking()
            .AnyAsync(
                lease => lease.ResidentId == residentId
                         && lease.UnitId == unitId
                         && lease.StartDate <= today
                         && (!lease.EndDate.HasValue || lease.EndDate.Value >= today),
                cancellationToken);
    }

    public async Task<IReadOnlyList<TicketOptionDalDto>> OptionsForTicketAsync(
        Guid managementCompanyId,
        Guid? unitId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Residents
            .AsNoTracking()
            .Where(resident => resident.ManagementCompanyId == managementCompanyId);

        if (unitId.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            query = query.Where(resident => resident.Leases!.Any(
                lease => lease.UnitId == unitId.Value
                         && lease.StartDate <= today
                         && (!lease.EndDate.HasValue || lease.EndDate.Value >= today)));
        }

        return await query
            .OrderBy(resident => resident.LastName)
            .ThenBy(resident => resident.FirstName)
            .Select(resident => new TicketOptionDalDto
            {
                Id = resident.Id,
                Label = resident.FirstName + " " + resident.LastName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaseResidentSearchItemDalDto>> SearchForLeaseAssignmentAsync(
        Guid managementCompanyId,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = searchTerm?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return Array.Empty<LeaseResidentSearchItemDalDto>();
        }

        var pattern = $"%{normalizedSearch}%";

        return await _dbContext.Residents
            .AsNoTracking()
            .Where(entity => entity.ManagementCompanyId == managementCompanyId)
            .Where(entity =>
                Microsoft.EntityFrameworkCore.EF.Functions.ILike(entity.FirstName, pattern) ||
                Microsoft.EntityFrameworkCore.EF.Functions.ILike(entity.LastName, pattern) ||
                Microsoft.EntityFrameworkCore.EF.Functions.ILike(entity.FirstName + " " + entity.LastName, pattern) ||
                Microsoft.EntityFrameworkCore.EF.Functions.ILike(entity.IdCode, pattern))
            .OrderBy(entity => entity.FirstName)
            .ThenBy(entity => entity.LastName)
            .ThenBy(entity => entity.IdCode)
            .Take(MaxLeaseAssignmentSearchResults)
            .Select(entity => new LeaseResidentSearchItemDalDto
            {
                ResidentId = entity.Id,
                FullName = string.Join(" ", new[] { entity.FirstName, entity.LastName }.Where(value => !string.IsNullOrWhiteSpace(value))),
                IdCode = entity.IdCode,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasDeleteDependenciesAsync(
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var leaseExists = await _dbContext.Leases
            .AsNoTracking()
            .AnyAsync(
                lease => lease.ResidentId == residentId
                         && lease.Resident!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (leaseExists)
        {
            return true;
        }

        var ticketExists = await _dbContext.Tickets
            .AsNoTracking()
            .AnyAsync(
                ticket => ticket.ResidentId == residentId
                          && ticket.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (ticketExists)
        {
            return true;
        }

        var residentUserExists = await _dbContext.ResidentUsers
            .AsNoTracking()
            .AnyAsync(
                residentUser => residentUser.ResidentId == residentId
                                && residentUser.Resident!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (residentUserExists)
        {
            return true;
        }

        var residentContactExists = await _dbContext.ResidentContacts
            .AsNoTracking()
            .AnyAsync(
                residentContact => residentContact.ResidentId == residentId
                                   && residentContact.Resident!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (residentContactExists)
        {
            return true;
        }

        return await _dbContext.CustomerRepresentatives
            .AsNoTracking()
            .AnyAsync(
                representative => representative.ResidentId == residentId
                                  && representative.Resident!.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }

    public async Task<bool> HasActiveUserResidentContextAsync(
        Guid appUserId,
        Guid residentId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _dbContext.ResidentUsers
            .AsNoTracking()
            .AnyAsync(
                residentUser => residentUser.AppUserId == appUserId
                                && residentUser.ResidentId == residentId
                                && residentUser.ValidFrom <= today
                                && (!residentUser.ValidTo.HasValue
                                    || residentUser.ValidTo.Value >= today),
                cancellationToken);
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _dbContext.Leases
            .AsNoTracking()
            .Where(entity => entity.ResidentId == residentId)
            .OrderByDescending(entity => entity.StartDate <= today && (!entity.EndDate.HasValue || entity.EndDate.Value >= today))
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
                Notes = entity.Notes == null ? null : entity.Notes.ToString()
            })
            .ToListAsync(cancellationToken);
    }

}
