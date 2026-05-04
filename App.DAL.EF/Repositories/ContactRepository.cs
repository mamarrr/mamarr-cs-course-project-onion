using App.DAL.Contracts.DAL.Contacts;
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

    public async Task<ContactDalDto?> FindAsync(
        Guid contactId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Contacts
            .AsNoTracking()
            .Select(entity => new ContactDalDto
            {
                Id = entity.Id,
                ManagementCompanyId = entity.ManagementCompanyId,
                ContactTypeId = entity.ContactTypeId,
                ContactValue = entity.ContactValue,
                Notes = entity.Notes == null ? null : entity.Notes.ToString()
            })
            .FirstOrDefaultAsync(entity => entity.Id == contactId, cancellationToken);
    }

    public Task<ContactDalDto> AddAsync(
        ContactCreateDalDto dto,
        CancellationToken cancellationToken = default)
    {
        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            ManagementCompanyId = dto.ManagementCompanyId,
            ContactTypeId = dto.ContactTypeId,
            ContactValue = dto.ContactValue,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : new LangStr(dto.Notes.Trim()),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Contacts.Add(contact);

        return Task.FromResult(new ContactDalDto
        {
            Id = contact.Id,
            ManagementCompanyId = contact.ManagementCompanyId,
            ContactTypeId = contact.ContactTypeId,
            ContactValue = contact.ContactValue,
            Notes = contact.Notes == null ? null : contact.Notes.ToString()
        });
    }

    public async Task UpdateAsync(
        ContactUpdateDalDto dto,
        CancellationToken cancellationToken = default)
    {
        var contact = await _dbContext.Contacts
            .AsTracking()
            .FirstOrDefaultAsync(
                entity => entity.Id == dto.Id && entity.ManagementCompanyId == dto.ManagementCompanyId,
                cancellationToken);

        if (contact is null)
        {
            return;
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
    }

    public async Task<bool> DeleteAsync(
        Guid contactId,
        CancellationToken cancellationToken = default)
    {
        var deleted = await _dbContext.Contacts
            .Where(entity => entity.Id == contactId)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }
}
