using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Residents;
using App.DAL.EF.Mappers.Residents;
using App.Domain;
using Base.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class ResidentContactRepository :
    BaseRepository<ResidentContactDalDto, ResidentContact, AppDbContext>,
    IResidentContactRepository
{
    private readonly AppDbContext _dbContext;

    public ResidentContactRepository(AppDbContext dbContext, ResidentContactDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
    }

    public override async Task<IEnumerable<ResidentContactDalDto>> AllAsync(
        Guid parentId = default,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ResidentContacts.AsNoTracking();
        if (parentId != default)
        {
            query = query.Where(entity => entity.Resident!.ManagementCompanyId == parentId);
        }

        var entities = await query.ToListAsync(cancellationToken);
        return entities.Select(entity => Mapper.Map(entity)!);
    }

    public override async Task<ResidentContactDalDto?> FindAsync(
        Guid id,
        Guid parentId = default,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ResidentContacts.AsNoTracking();
        if (parentId != default)
        {
            query = query.Where(entity => entity.Resident!.ManagementCompanyId == parentId);
        }

        var entity = await query.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
        return Mapper.Map(entity);
    }

    public override async Task RemoveAsync(
        Guid id,
        Guid parentId = default,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ResidentContacts.AsTracking();
        if (parentId != default)
        {
            query = query.Where(entity => entity.Resident!.ManagementCompanyId == parentId);
        }

        var entity = await query.FirstOrDefaultAsync(residentContact => residentContact.Id == id, cancellationToken);
        if (entity is not null)
        {
            _dbContext.ResidentContacts.Remove(entity);
        }
    }

    public async Task<IReadOnlyList<ResidentContactAssignmentDalDto>> AllByResidentAsync(
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ResidentContacts
            .AsNoTracking()
            .Where(entity => entity.ResidentId == residentId
                             && entity.Resident!.ManagementCompanyId == managementCompanyId)
            .OrderByDescending(entity => entity.IsPrimary)
            .ThenBy(entity => entity.Contact!.ContactType!.Code)
            .ThenBy(entity => entity.Contact!.ContactValue)
            .Select(entity => new ResidentContactAssignmentDalDto
            {
                Id = entity.Id,
                ResidentId = entity.ResidentId,
                ContactId = entity.ContactId,
                ContactTypeId = entity.Contact!.ContactTypeId,
                ContactTypeCode = entity.Contact.ContactType!.Code,
                ContactTypeLabel = entity.Contact.ContactType.Label.ToString(),
                ContactValue = entity.Contact.ContactValue,
                ContactNotes = entity.Contact.Notes == null ? null : entity.Contact.Notes.ToString(),
                ValidFrom = entity.ValidFrom,
                ValidTo = entity.ValidTo,
                Confirmed = entity.Confirmed,
                IsPrimary = entity.IsPrimary,
                CreatedAt = entity.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ResidentContactDalDto?> FindInCompanyAsync(
        Guid residentContactId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ResidentContacts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                residentContact => residentContact.Id == residentContactId
                                  && residentContact.Resident!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        return Mapper.Map(entity);
    }

    public Task<bool> ExistsInCompanyAsync(
        Guid residentContactId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ResidentContacts
            .AsNoTracking()
            .AnyAsync(
                residentContact => residentContact.Id == residentContactId
                                   && residentContact.Resident!.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }

    public Task<bool> HasPrimaryAsync(
        Guid residentId,
        Guid managementCompanyId,
        Guid? exceptResidentContactId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ResidentContacts
            .AsNoTracking()
            .Where(entity => entity.ResidentId == residentId
                             && entity.Resident!.ManagementCompanyId == managementCompanyId
                             && entity.IsPrimary)
            .Where(entity => exceptResidentContactId == null || entity.Id != exceptResidentContactId.Value)
            .AnyAsync(cancellationToken);
    }

    public async Task ClearPrimaryAsync(
        Guid residentId,
        Guid managementCompanyId,
        Guid? exceptResidentContactId = null,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.ResidentContacts
            .Where(entity => entity.ResidentId == residentId
                             && entity.Resident!.ManagementCompanyId == managementCompanyId
                             && entity.IsPrimary)
            .Where(entity => exceptResidentContactId == null || entity.Id != exceptResidentContactId.Value)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(entity => entity.IsPrimary, false),
                cancellationToken);
    }

    public Task<bool> ContactLinkedToResidentAsync(
        Guid residentId,
        Guid contactId,
        Guid managementCompanyId,
        Guid? exceptResidentContactId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ResidentContacts
            .AsNoTracking()
            .Where(entity => entity.ResidentId == residentId
                             && entity.ContactId == contactId
                             && entity.Resident!.ManagementCompanyId == managementCompanyId)
            .Where(entity => exceptResidentContactId == null || entity.Id != exceptResidentContactId.Value)
            .AnyAsync(cancellationToken);
    }

    public override async Task<ResidentContactDalDto> UpdateAsync(
        ResidentContactDalDto dto,
        Guid parentId = default,
        CancellationToken cancellationToken = default)
    {
        var managementCompanyId = parentId;
        var entity = await _dbContext.ResidentContacts
            .AsTracking()
            .FirstOrDefaultAsync(
                residentContact => residentContact.Id == dto.Id
                                   && residentContact.ResidentId == dto.ResidentId
                                   && residentContact.Resident!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (entity is null)
        {
            throw new ApplicationException($"Resident contact with id {dto.Id} was not found.");
        }

        entity.ContactId = dto.ContactId;
        entity.ValidFrom = dto.ValidFrom;
        entity.ValidTo = dto.ValidTo;
        entity.Confirmed = dto.Confirmed;
        entity.IsPrimary = dto.IsPrimary;

        return Mapper.Map(entity)!;
    }

    public async Task<bool> DeleteInCompanyAsync(
        Guid residentContactId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ResidentContacts
            .AsTracking()
            .FirstOrDefaultAsync(
                residentContact => residentContact.Id == residentContactId
                                  && residentContact.Resident!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _dbContext.ResidentContacts.Remove(entity);
        return true;
    }
}
