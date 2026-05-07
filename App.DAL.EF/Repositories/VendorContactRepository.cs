using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Vendors;
using App.DAL.EF.Mappers.Vendors;
using App.Domain;
using Base.DAL.EF;
using Base.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class VendorContactRepository :
    BaseRepository<VendorContactDalDto, VendorContact, AppDbContext>,
    IVendorContactRepository
{
    private readonly AppDbContext _dbContext;

    public VendorContactRepository(AppDbContext dbContext, VendorContactDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
    }

    public override async Task<IEnumerable<VendorContactDalDto>> AllAsync(
        Guid parentId = default,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.VendorContacts.AsNoTracking();
        if (parentId != default)
        {
            query = query.Where(entity => entity.Vendor!.ManagementCompanyId == parentId);
        }

        var entities = await query.ToListAsync(cancellationToken);
        return entities.Select(entity => Mapper.Map(entity)!);
    }

    public override async Task<VendorContactDalDto?> FindAsync(
        Guid id,
        Guid parentId = default,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.VendorContacts.AsNoTracking();
        if (parentId != default)
        {
            query = query.Where(entity => entity.Vendor!.ManagementCompanyId == parentId);
        }

        var entity = await query.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
        return Mapper.Map(entity);
    }

    public override async Task RemoveAsync(
        Guid id,
        Guid parentId = default,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.VendorContacts.AsTracking();
        if (parentId != default)
        {
            query = query.Where(entity => entity.Vendor!.ManagementCompanyId == parentId);
        }

        var entity = await query.FirstOrDefaultAsync(vendorContact => vendorContact.Id == id, cancellationToken);
        if (entity is not null)
        {
            _dbContext.VendorContacts.Remove(entity);
        }
    }

    public async Task<IReadOnlyList<VendorContactAssignmentDalDto>> AllByVendorAsync(
        Guid vendorId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.VendorContacts
            .AsNoTracking()
            .Where(entity => entity.VendorId == vendorId
                             && entity.Vendor!.ManagementCompanyId == managementCompanyId)
            .OrderByDescending(entity => entity.IsPrimary)
            .ThenBy(entity => entity.FullName ?? entity.Contact!.ContactValue)
            .Select(entity => new VendorContactAssignmentDalDto
            {
                Id = entity.Id,
                VendorId = entity.VendorId,
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
                FullName = entity.FullName,
                RoleTitle = entity.RoleTitle == null ? null : entity.RoleTitle.ToString(),
                CreatedAt = entity.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<VendorContactDalDto?> FindInCompanyAsync(
        Guid vendorContactId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.VendorContacts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                vendorContact => vendorContact.Id == vendorContactId
                                 && vendorContact.Vendor!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        return Mapper.Map(entity);
    }

    public Task<bool> ExistsInCompanyAsync(
        Guid vendorContactId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.VendorContacts
            .AsNoTracking()
            .AnyAsync(
                vendorContact => vendorContact.Id == vendorContactId
                                 && vendorContact.Vendor!.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }

    public Task<bool> HasPrimaryAsync(
        Guid vendorId,
        Guid managementCompanyId,
        Guid? exceptVendorContactId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.VendorContacts
            .AsNoTracking()
            .Where(entity => entity.VendorId == vendorId
                             && entity.Vendor!.ManagementCompanyId == managementCompanyId
                             && entity.IsPrimary)
            .Where(entity => exceptVendorContactId == null || entity.Id != exceptVendorContactId.Value)
            .AnyAsync(cancellationToken);
    }

    public async Task ClearPrimaryAsync(
        Guid vendorId,
        Guid managementCompanyId,
        Guid? exceptVendorContactId = null,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.VendorContacts
            .Where(entity => entity.VendorId == vendorId
                             && entity.Vendor!.ManagementCompanyId == managementCompanyId
                             && entity.IsPrimary)
            .Where(entity => exceptVendorContactId == null || entity.Id != exceptVendorContactId.Value)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(entity => entity.IsPrimary, false),
                cancellationToken);
    }

    public Task<bool> ContactLinkedToVendorAsync(
        Guid vendorId,
        Guid contactId,
        Guid managementCompanyId,
        Guid? exceptVendorContactId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.VendorContacts
            .AsNoTracking()
            .Where(entity => entity.VendorId == vendorId
                             && entity.ContactId == contactId
                             && entity.Vendor!.ManagementCompanyId == managementCompanyId)
            .Where(entity => exceptVendorContactId == null || entity.Id != exceptVendorContactId.Value)
            .AnyAsync(cancellationToken);
    }

    public override async Task<VendorContactDalDto> UpdateAsync(
        VendorContactDalDto dto,
        Guid parentId = default,
        CancellationToken cancellationToken = default)
    {
        var managementCompanyId = parentId;
        var entity = await _dbContext.VendorContacts
            .AsTracking()
            .FirstOrDefaultAsync(
                vendorContact => vendorContact.Id == dto.Id
                                 && vendorContact.VendorId == dto.VendorId
                                 && vendorContact.Vendor!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (entity is null)
        {
            throw new ApplicationException($"Vendor contact with id {dto.Id} was not found.");
        }

        entity.ContactId = dto.ContactId;
        entity.ValidFrom = dto.ValidFrom;
        entity.ValidTo = dto.ValidTo;
        entity.Confirmed = dto.Confirmed;
        entity.IsPrimary = dto.IsPrimary;
        entity.FullName = dto.FullName;

        if (string.IsNullOrWhiteSpace(dto.RoleTitle))
        {
            entity.RoleTitle = null;
            _dbContext.Entry(entity).Property(vendorContact => vendorContact.RoleTitle).IsModified = true;
        }
        else if (entity.RoleTitle is null)
        {
            entity.RoleTitle = new LangStr(dto.RoleTitle.Trim());
            _dbContext.Entry(entity).Property(vendorContact => vendorContact.RoleTitle).IsModified = true;
        }
        else
        {
            entity.RoleTitle.SetTranslation(dto.RoleTitle.Trim());
            _dbContext.Entry(entity).Property(vendorContact => vendorContact.RoleTitle).IsModified = true;
        }

        return Mapper.Map(entity)!;
    }

    public async Task<bool> DeleteInCompanyAsync(
        Guid vendorContactId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.VendorContacts
            .AsTracking()
            .FirstOrDefaultAsync(
                vendorContact => vendorContact.Id == vendorContactId
                                 && vendorContact.Vendor!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _dbContext.VendorContacts.Remove(entity);
        return true;
    }
}
