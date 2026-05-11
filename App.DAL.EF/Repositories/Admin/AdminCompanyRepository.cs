using App.DAL.Contracts.Repositories.Admin;
using App.DAL.DTO.Admin.Companies;
using App.DAL.EF.Mappers.Admin;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories.Admin;

public class AdminCompanyRepository : IAdminCompanyRepository
{
    private readonly AppDbContext _dbContext;
    private readonly AdminCompanyDalMapper _mapper;

    public AdminCompanyRepository(AppDbContext dbContext, AdminCompanyDalMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<AdminCompanyListItemDalDto>> SearchCompaniesAsync(AdminCompanySearchDalDto search, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ManagementCompanies.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search.SearchText))
        {
            var term = search.SearchText.Trim().ToUpperInvariant();
            query = query.Where(company =>
                company.Name.ToUpper().Contains(term) ||
                company.RegistryCode.ToUpper().Contains(term) ||
                company.Slug.ToUpper().Contains(term) ||
                company.Email.ToUpper().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            var name = search.Name.Trim().ToUpperInvariant();
            query = query.Where(company => company.Name.ToUpper().Contains(name));
        }

        if (!string.IsNullOrWhiteSpace(search.RegistryCode))
        {
            var registryCode = search.RegistryCode.Trim().ToUpperInvariant();
            query = query.Where(company => company.RegistryCode.ToUpper().Contains(registryCode));
        }

        if (!string.IsNullOrWhiteSpace(search.Slug))
        {
            var slug = search.Slug.Trim().ToUpperInvariant();
            query = query.Where(company => company.Slug.ToUpper().Contains(slug));
        }

        var companies = await query
            .OrderBy(company => company.Name)
            .ToListAsync(cancellationToken);

        var result = new List<AdminCompanyListItemDalDto>();
        foreach (var company in companies)
        {
            result.Add(_mapper.MapListItem(
                company,
                await _dbContext.ManagementCompanyUsers.CountAsync(user => user.ManagementCompanyId == company.Id, cancellationToken),
                await _dbContext.Tickets.CountAsync(ticket => ticket.ManagementCompanyId == company.Id && ticket.ClosedAt == null, cancellationToken)));
        }

        return result;
    }

    public async Task<AdminCompanyDetailsDalDto?> GetCompanyDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var company = await _dbContext.ManagementCompanies
            .AsNoTracking()
            .FirstOrDefaultAsync(company => company.Id == id, cancellationToken);

        if (company is null)
        {
            return null;
        }

        var customerIds = _dbContext.Customers
            .AsNoTracking()
            .Where(customer => customer.ManagementCompanyId == id)
            .Select(customer => customer.Id);

        var propertyIds = _dbContext.Properties
            .AsNoTracking()
            .Where(property => customerIds.Contains(property.CustomerId))
            .Select(property => property.Id);

        return _mapper.MapDetails(
            company,
            await _dbContext.ManagementCompanyUsers.CountAsync(user => user.ManagementCompanyId == id, cancellationToken),
            await _dbContext.Tickets.CountAsync(ticket => ticket.ManagementCompanyId == id && ticket.ClosedAt == null, cancellationToken),
            await customerIds.CountAsync(cancellationToken),
            await propertyIds.CountAsync(cancellationToken),
            await _dbContext.Units.CountAsync(unit => propertyIds.Contains(unit.PropertyId), cancellationToken),
            await _dbContext.Residents.CountAsync(resident => resident.ManagementCompanyId == id, cancellationToken),
            await _dbContext.Tickets.CountAsync(ticket => ticket.ManagementCompanyId == id, cancellationToken),
            await _dbContext.Vendors.CountAsync(vendor => vendor.ManagementCompanyId == id, cancellationToken),
            await _dbContext.ManagementCompanyJoinRequests.CountAsync(request =>
                request.ManagementCompanyId == id &&
                request.ManagementCompanyJoinRequestStatus != null &&
                request.ManagementCompanyJoinRequestStatus.Code == "PENDING", cancellationToken));
    }

    public Task<bool> SlugExistsAsync(string slug, Guid? exceptId = null, CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim();
        return _dbContext.ManagementCompanies
            .AsNoTracking()
            .AnyAsync(company => company.Slug == normalized && (!exceptId.HasValue || company.Id != exceptId.Value), cancellationToken);
    }

    public Task<bool> RegistryCodeExistsAsync(string registryCode, Guid? exceptId = null, CancellationToken cancellationToken = default)
    {
        var normalized = registryCode.Trim();
        return _dbContext.ManagementCompanies
            .AsNoTracking()
            .AnyAsync(company => company.RegistryCode == normalized && (!exceptId.HasValue || company.Id != exceptId.Value), cancellationToken);
    }

    public async Task<bool> UpdateCompanyAsync(Guid id, AdminCompanyUpdateDalDto dto, CancellationToken cancellationToken = default)
    {
        var company = await _dbContext.ManagementCompanies
            .AsTracking()
            .FirstOrDefaultAsync(company => company.Id == id, cancellationToken);
        if (company is null)
        {
            return false;
        }

        company.Name = dto.Name.Trim();
        company.RegistryCode = dto.RegistryCode.Trim();
        company.VatNumber = dto.VatNumber.Trim();
        company.Email = dto.Email.Trim();
        company.Phone = dto.Phone.Trim();
        company.Address = dto.Address.Trim();
        company.Slug = dto.Slug.Trim();
        return true;
    }
}
