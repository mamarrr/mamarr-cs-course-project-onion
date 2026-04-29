using App.Contracts.DAL.Contacts;
using App.DAL.EF.Mappers.Contacts;
using App.Domain;
using Base.DAL.EF;
using Base.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public sealed class ContactRepository :
    BaseRepository<ContactDalDto, Contact, AppDbContext>,
    IContactRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ContactDalMapper _mapper;

    public ContactRepository(AppDbContext dbContext, ContactDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ContactDalDto?> FindAsync(
        Guid contactId,
        CancellationToken cancellationToken = default)
    {
        var contact = await _dbContext.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == contactId, cancellationToken);

        return _mapper.Map(contact);
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

        return Task.FromResult(_mapper.Map(contact)!);
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
