using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Tickets;
using App.DAL.DTO.Vendors;
using App.DAL.EF.Mappers.Vendors;
using App.Domain;
using Base.DAL.EF;
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
        vendor.Notes.SetTranslation(dto.Notes.Trim());
        _dbContext.Entry(vendor).Property(entity => entity.Notes).IsModified = true;

        return Mapper.Map(vendor)!;
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
