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
