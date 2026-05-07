using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Tickets;
using App.DAL.DTO.Vendors;
using App.DAL.EF.Mappers.Vendors;
using App.Domain;
using Base.DAL.EF;
using Base.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class VendorRepository :
    BaseRepository<VendorDalDto, Vendor, AppDbContext>,
    IVendorRepository
{
    private readonly AppDbContext _dbContext;

    public VendorRepository(AppDbContext dbContext, VendorDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
    }

    public override async Task<VendorDalDto> UpdateAsync(
        VendorDalDto dto,
        Guid parentId = default,
        CancellationToken cancellationToken = default)
    {
        var managementCompanyId = parentId == default ? dto.ManagementCompanyId : parentId;

        var vendor = await _dbContext.Vendors
            .AsTracking()
            .FirstOrDefaultAsync(
                entity => entity.Id == dto.Id && entity.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (vendor is null)
        {
            throw new ApplicationException($"Vendor with id {dto.Id} was not found.");
        }

        vendor.Name = dto.Name;
        vendor.RegistryCode = dto.RegistryCode;
        if (vendor.Notes is null)
        {
            vendor.Notes = new LangStr(dto.Notes.Trim());
        }
        else
        {
            vendor.Notes.SetTranslation(dto.Notes.Trim());
        }
        _dbContext.Entry(vendor).Property(entity => entity.Notes).IsModified = true;

        return Mapper.Map(vendor)!;
    }

    public async Task<IReadOnlyList<VendorListItemDalDto>> AllByCompanyAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Vendors
            .AsNoTracking()
            .Where(vendor => vendor.ManagementCompanyId == managementCompanyId)
            .OrderBy(vendor => vendor.Name)
            .Select(vendor => new VendorListItemDalDto
            {
                Id = vendor.Id,
                ManagementCompanyId = vendor.ManagementCompanyId,
                Name = vendor.Name,
                RegistryCode = vendor.RegistryCode,
                CreatedAt = vendor.CreatedAt,
                ActiveCategoryCount = vendor.VendorTicketCategories!.Count,
                AssignedTicketCount = vendor.Tickets!.Count(ticket => ticket.ManagementCompanyId == managementCompanyId),
                ContactCount = vendor.VendorContacts!.Count,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<VendorProfileDalDto?> FindProfileAsync(
        Guid vendorId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Vendors
            .AsNoTracking()
            .Where(vendor => vendor.Id == vendorId && vendor.ManagementCompanyId == managementCompanyId)
            .Select(vendor => new VendorProfileDalDto
            {
                Id = vendor.Id,
                ManagementCompanyId = vendor.ManagementCompanyId,
                CompanySlug = vendor.ManagementCompany!.Slug,
                CompanyName = vendor.ManagementCompany.Name,
                Name = vendor.Name,
                RegistryCode = vendor.RegistryCode,
                Notes = vendor.Notes.ToString(),
                CreatedAt = vendor.CreatedAt,
                ActiveCategoryCount = vendor.VendorTicketCategories!.Count,
                AssignedTicketCount = vendor.Tickets!.Count(ticket => ticket.ManagementCompanyId == managementCompanyId),
                ContactCount = vendor.VendorContacts!.Count,
                ScheduledWorkCount = vendor.ScheduledWorks!.Count
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> RegistryCodeExistsInCompanyAsync(
        Guid managementCompanyId,
        string registryCode,
        Guid? exceptVendorId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedRegistryCode = registryCode.Trim().ToLower();

        return await _dbContext.Vendors
            .AsNoTracking()
            .Where(vendor => vendor.ManagementCompanyId == managementCompanyId)
            .Where(vendor => exceptVendorId == null || vendor.Id != exceptVendorId.Value)
            .AnyAsync(vendor => vendor.RegistryCode.ToLower() == normalizedRegistryCode, cancellationToken);
    }

    public async Task<bool> HasDeleteDependenciesAsync(
        Guid vendorId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var ticketsExist = await _dbContext.Tickets
            .AsNoTracking()
            .AnyAsync(
                ticket => ticket.ManagementCompanyId == managementCompanyId
                          && ticket.VendorId == vendorId,
                cancellationToken);
        if (ticketsExist)
        {
            return true;
        }

        var scheduledWorkExists = await _dbContext.ScheduledWorks
            .AsNoTracking()
            .AnyAsync(
                work => work.VendorId == vendorId
                        && work.Vendor!.ManagementCompanyId == managementCompanyId,
                cancellationToken);
        if (scheduledWorkExists)
        {
            return true;
        }

        var contactExists = await _dbContext.VendorContacts
            .AsNoTracking()
            .AnyAsync(
                contact => contact.VendorId == vendorId
                           && contact.Vendor!.ManagementCompanyId == managementCompanyId,
                cancellationToken);
        if (contactExists)
        {
            return true;
        }

        return await _dbContext.VendorTicketCategories
            .AsNoTracking()
            .AnyAsync(
                category => category.VendorId == vendorId
                            && category.Vendor!.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }

    public Task<bool> ExistsInCompanyAsync(
        Guid vendorId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Vendors
            .AsNoTracking()
            .AnyAsync(
                vendor => vendor.Id == vendorId && vendor.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<TicketOptionDalDto>> OptionsForTicketAsync(
        Guid managementCompanyId,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Vendors
            .AsNoTracking()
            .Where(vendor => vendor.ManagementCompanyId == managementCompanyId);

        if (categoryId.HasValue)
        {
            query = query.Where(vendor => vendor.VendorTicketCategories!
                .Any(link => link.TicketCategoryId == categoryId.Value));
        }

        return await query
            .OrderBy(vendor => vendor.Name)
            .Select(vendor => new TicketOptionDalDto
            {
                Id = vendor.Id,
                Label = vendor.Name
            })
            .ToListAsync(cancellationToken);
    }
}
