using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Contacts;
using App.DAL.EF.Mappers.Contacts;
using App.Domain;
using Base.DAL.EF;
using Base.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class ContactRepository :
    BaseRepository<ContactDalDto, Contact, AppDbContext>,
    IContactRepository
{
    private readonly AppDbContext _dbContext;

    public ContactRepository(AppDbContext dbContext, ContactDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
    }

    public async Task<ContactDalDto?> FindInCompanyAsync(
        Guid contactId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var contact = await _dbContext.Contacts
            .AsNoTracking()
            .Where(contact => contact.Id == contactId && contact.ManagementCompanyId == managementCompanyId)
            .FirstOrDefaultAsync(cancellationToken);

        return Mapper.Map(contact);
    }

    public Task<bool> ExistsInCompanyAsync(
        Guid contactId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Contacts
            .AsNoTracking()
            .AnyAsync(
                contact => contact.Id == contactId && contact.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ContactDalDto>> OptionsByCompanyAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var contacts = await _dbContext.Contacts
            .AsNoTracking()
            .Where(contact => contact.ManagementCompanyId == managementCompanyId)
            .OrderBy(contact => contact.ContactType!.Code)
            .ThenBy(contact => contact.ContactValue)
            .ToListAsync(cancellationToken);

        return contacts.Select(contact => Mapper.Map(contact)!).ToList();
    }

    public async Task<bool> DuplicateValueExistsAsync(
        Guid managementCompanyId,
        Guid contactTypeId,
        string contactValue,
        Guid? exceptContactId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedValue = contactValue.Trim().ToLower();

        return await _dbContext.Contacts
            .AsNoTracking()
            .Where(contact => contact.ManagementCompanyId == managementCompanyId)
            .Where(contact => contact.ContactTypeId == contactTypeId)
            .Where(contact => exceptContactId == null || contact.Id != exceptContactId.Value)
            .AnyAsync(contact => contact.ContactValue.ToLower() == normalizedValue, cancellationToken);
    }

    public async Task<bool> HasDependenciesAsync(
        Guid contactId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var vendorContactExists = await _dbContext.VendorContacts
            .AsNoTracking()
            .AnyAsync(
                vendorContact => vendorContact.ContactId == contactId
                                 && vendorContact.Contact!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (vendorContactExists)
        {
            return true;
        }

        return await _dbContext.ResidentContacts
            .AsNoTracking()
            .AnyAsync(
                residentContact => residentContact.ContactId == contactId
                                   && residentContact.Contact!.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }

    public override async Task<ContactDalDto> UpdateAsync(
        ContactDalDto dto,
        Guid parentId = default,
        CancellationToken cancellationToken = default)
    {
        var managementCompanyId = parentId == default ? dto.ManagementCompanyId : parentId;

        var contact = await _dbContext.Contacts
            .AsTracking()
            .FirstOrDefaultAsync(
                entity => entity.Id == dto.Id && entity.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (contact is null)
        {
            throw new ApplicationException($"Contact with id {dto.Id} was not found.");
        }

        contact.ContactTypeId = dto.ContactTypeId;
        contact.ContactValue = dto.ContactValue;

        if (string.IsNullOrWhiteSpace(dto.Notes))
        {
            contact.Notes = null;
            _dbContext.Entry(contact).Property(entity => entity.Notes).IsModified = true;
        }
        else if (contact.Notes is null)
        {
            contact.Notes = new LangStr(dto.Notes.Trim());
            _dbContext.Entry(contact).Property(entity => entity.Notes).IsModified = true;
        }
        else
        {
            contact.Notes.SetTranslation(dto.Notes.Trim());
            _dbContext.Entry(contact).Property(entity => entity.Notes).IsModified = true;
        }

        return Mapper.Map(contact)!;
    }
}
