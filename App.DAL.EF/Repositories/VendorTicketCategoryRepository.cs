using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Vendors;
using App.DAL.EF.Mappers.Vendors;
using App.Domain;
using Base.DAL.EF;
using Base.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class VendorTicketCategoryRepository :
    BaseRepository<VendorTicketCategoryDalDto, VendorTicketCategory, AppDbContext>,
    IVendorTicketCategoryRepository
{
    private readonly AppDbContext _dbContext;

    public VendorTicketCategoryRepository(AppDbContext dbContext, VendorTicketCategoryDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<VendorCategoryAssignmentDalDto>> AllByVendorAsync(
        Guid vendorId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.VendorTicketCategories
            .AsNoTracking()
            .Where(assignment => assignment.VendorId == vendorId
                                 && assignment.Vendor!.ManagementCompanyId == managementCompanyId)
            .OrderBy(assignment => assignment.TicketCategory!.Code)
            .Select(assignment => new VendorCategoryAssignmentDalDto
            {
                Id = assignment.Id,
                VendorId = assignment.VendorId,
                TicketCategoryId = assignment.TicketCategoryId,
                CategoryCode = assignment.TicketCategory!.Code,
                CategoryLabel = assignment.TicketCategory.Label.ToString(),
                Notes = assignment.Notes == null ? null : assignment.Notes.ToString(),
                CreatedAt = assignment.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<VendorTicketCategoryDalDto?> FindInCompanyAsync(
        Guid vendorTicketCategoryId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _dbContext.VendorTicketCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                entity => entity.Id == vendorTicketCategoryId
                          && entity.Vendor!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        return Mapper.Map(assignment);
    }

    public async Task<VendorTicketCategoryDalDto?> FindByVendorCategoryInCompanyAsync(
        Guid vendorId,
        Guid ticketCategoryId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _dbContext.VendorTicketCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                entity => entity.VendorId == vendorId
                          && entity.TicketCategoryId == ticketCategoryId
                          && entity.Vendor!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        return Mapper.Map(assignment);
    }

    public Task<bool> ExistsAsync(
        Guid vendorId,
        Guid ticketCategoryId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.VendorTicketCategories
            .AsNoTracking()
            .AnyAsync(
                assignment => assignment.VendorId == vendorId
                              && assignment.TicketCategoryId == ticketCategoryId,
                cancellationToken);
    }

    public Task<bool> ExistsInCompanyAsync(
        Guid vendorTicketCategoryId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.VendorTicketCategories
            .AsNoTracking()
            .AnyAsync(
                assignment => assignment.Id == vendorTicketCategoryId
                              && assignment.Vendor!.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }

    public Task<bool> ExistsInCompanyAsync(
        Guid vendorId,
        Guid ticketCategoryId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.VendorTicketCategories
            .AsNoTracking()
            .AnyAsync(
                assignment => assignment.VendorId == vendorId
                              && assignment.TicketCategoryId == ticketCategoryId
                              && assignment.Vendor!.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }

    public override async Task<VendorTicketCategoryDalDto> UpdateAsync(
        VendorTicketCategoryDalDto dto,
        Guid parentId = default,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _dbContext.VendorTicketCategories
            .AsTracking()
            .FirstOrDefaultAsync(
                entity => entity.Id == dto.Id
                          && entity.VendorId == dto.VendorId
                          && entity.TicketCategoryId == dto.TicketCategoryId
                          && entity.Vendor!.ManagementCompanyId == parentId,
                cancellationToken);

        if (assignment is null)
        {
            throw new ApplicationException($"Vendor ticket category assignment with id {dto.Id} was not found.");
        }

        var notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
        if (notes is null)
        {
            assignment.Notes = null;
        }
        else if (assignment.Notes is null)
        {
            assignment.Notes = new LangStr(notes);
        }
        else
        {
            assignment.Notes.SetTranslation(notes);
        }

        _dbContext.Entry(assignment).Property(entity => entity.Notes).IsModified = true;
        return Mapper.Map(assignment)!;
    }

    public async Task<bool> DeleteAssignmentAsync(
        Guid vendorId,
        Guid ticketCategoryId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _dbContext.VendorTicketCategories
            .AsTracking()
            .FirstOrDefaultAsync(
                entity => entity.VendorId == vendorId
                          && entity.TicketCategoryId == ticketCategoryId
                          && entity.Vendor!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (assignment is null)
        {
            return false;
        }

        _dbContext.VendorTicketCategories.Remove(assignment);
        return true;
    }
}
