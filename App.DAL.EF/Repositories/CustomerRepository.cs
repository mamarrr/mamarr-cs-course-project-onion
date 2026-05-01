using App.Contracts.DAL.Customers;
using App.DAL.EF.Mappers.Customers;
using App.Domain;
using Base.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class CustomerRepository :
    BaseRepository<CustomerDalDto, Customer, AppDbContext>,
    ICustomerRepository
{
    private readonly AppDbContext _dbContext;

    public CustomerRepository(AppDbContext dbContext, CustomerDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CustomerListItemDalDto>> AllByCompanySlugAsync(
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanySlug = companySlug.Trim();

        var customers = await _dbContext.Customers
            .AsNoTracking()
            .Where(c => c.ManagementCompany!.Slug == normalizedCompanySlug)
            .OrderBy(c => c.Name)
            .Select(c => new CustomerListItemDalDto
            {
                Id = c.Id,
                ManagementCompanyId = c.ManagementCompanyId,
                Name = c.Name,
                Slug = c.Slug,
                RegistryCode = c.RegistryCode,
                BillingEmail = c.BillingEmail,
                BillingAddress = c.BillingAddress,
                Phone = c.Phone,
                IsActive = c.IsActive
            })
            .ToListAsync(cancellationToken);

        return customers;
    }

    public async Task<IReadOnlyList<CustomerListItemDalDto>> AllByCompanyIdAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var customers = await _dbContext.Customers
            .AsNoTracking()
            .Where(c => c.ManagementCompanyId == managementCompanyId)
            .OrderBy(c => c.Name)
            .Select(c => new CustomerListItemDalDto
            {
                Id = c.Id,
                ManagementCompanyId = c.ManagementCompanyId,
                Name = c.Name,
                Slug = c.Slug,
                RegistryCode = c.RegistryCode,
                BillingEmail = c.BillingEmail,
                BillingAddress = c.BillingAddress,
                Phone = c.Phone,
                IsActive = c.IsActive
            })
            .ToListAsync(cancellationToken);

        return customers;
    }

    public async Task<IReadOnlyList<CustomerPropertyLinkDalDto>> AllPropertyLinksByCompanyIdAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var properties = await _dbContext.Properties
            .AsNoTracking()
            .Where(p => p.Customer!.ManagementCompanyId == managementCompanyId)
            .OrderBy(p => p.Label)
            .Select(p => new CustomerPropertyLinkDalDto
            {
                CustomerId = p.CustomerId,
                PropertySlug = p.Slug,
                PropertyName = p.Label.ToString()
            })
            .ToListAsync(cancellationToken);

        return properties;
    }

    public Task<CustomerDalDto> AddAsync(
        CustomerCreateDalDto dto,
        CancellationToken cancellationToken = default)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            ManagementCompanyId = dto.ManagementCompanyId,
            Name = dto.Name,
            Slug = dto.Slug,
            RegistryCode = dto.RegistryCode,
            BillingEmail = dto.BillingEmail,
            BillingAddress = dto.BillingAddress,
            Phone = dto.Phone,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Customers.Add(customer);

        return Task.FromResult(new CustomerDalDto
        {
            Id = customer.Id,
            ManagementCompanyId = customer.ManagementCompanyId,
            Name = customer.Name,
            Slug = customer.Slug,
            RegistryCode = customer.RegistryCode,
            IsActive = customer.IsActive
        });
    }

    public async Task<bool> CustomerSlugExistsInCompanyAsync(
        Guid managementCompanyId,
        string slug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLower();

        return await _dbContext.Customers
            .AsNoTracking()
            .Where(c => c.ManagementCompanyId == managementCompanyId)
            .AnyAsync(c => c.Slug.ToLower() == normalizedSlug, cancellationToken);
    }

    public async Task<CustomerWorkspaceDalDto?> FirstWorkspaceByCompanyAndSlugAsync(
        Guid managementCompanyId,
        string customerSlug,
        CancellationToken cancellationToken = default)
    {
        var normalizedCustomerSlug = customerSlug.Trim();

        var customer = await _dbContext.Customers
            .AsNoTracking()
            .Where(c => c.ManagementCompanyId == managementCompanyId && c.Slug == normalizedCustomerSlug)
            .Select(c => new CustomerWorkspaceDalDto
            {
                Id = c.Id,
                ManagementCompanyId = c.ManagementCompanyId,
                CompanySlug = c.ManagementCompany!.Slug,
                CompanyName = c.ManagementCompany.Name,
                Name = c.Name,
                Slug = c.Slug,
                IsActive = c.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        return customer;
    }

    public async Task<IReadOnlyList<CustomerUserContextDalDto>> ActiveUserCustomerContextsAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        return await (
                from residentUser in _dbContext.ResidentUsers.AsNoTracking()
                join customerRepresentative in _dbContext.CustomerRepresentatives.AsNoTracking()
                    on residentUser.ResidentId equals customerRepresentative.ResidentId
                join customer in _dbContext.Customers.AsNoTracking()
                    on customerRepresentative.CustomerId equals customer.Id
                where residentUser.AppUserId == appUserId
                      && residentUser.IsActive
                      && customerRepresentative.IsActive
                      && customer.IsActive
                select new CustomerUserContextDalDto
                {
                    CustomerId = customer.Id,
                    Name = customer.Name
                })
            .Distinct()
            .OrderBy(customer => customer.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ActiveUserCustomerContextExistsAsync(
        Guid appUserId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await (
                from residentUser in _dbContext.ResidentUsers.AsNoTracking()
                join customerRepresentative in _dbContext.CustomerRepresentatives.AsNoTracking()
                    on residentUser.ResidentId equals customerRepresentative.ResidentId
                where residentUser.AppUserId == appUserId
                      && residentUser.IsActive
                      && customerRepresentative.IsActive
                      && customerRepresentative.CustomerId == customerId
                select customerRepresentative.Id)
            .AnyAsync(cancellationToken);
    }

    public async Task<CustomerProfileDalDto?> FirstProfileByCompanyAndSlugAsync(
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken = default)
    {
        var normalizedCompanySlug = companySlug.Trim();
        var normalizedCustomerSlug = customerSlug.Trim();

        var customer = await _dbContext.Customers
            .AsNoTracking()
            .Where(c => c.ManagementCompany!.Slug == normalizedCompanySlug && c.Slug == normalizedCustomerSlug)
            .Select(c => new CustomerProfileDalDto
            {
                Id = c.Id,
                ManagementCompanyId = c.ManagementCompanyId,
                CompanySlug = c.ManagementCompany!.Slug,
                CompanyName = c.ManagementCompany.Name,
                Name = c.Name,
                Slug = c.Slug,
                RegistryCode = c.RegistryCode,
                BillingEmail = c.BillingEmail,
                BillingAddress = c.BillingAddress,
                Phone = c.Phone,
                IsActive = c.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        return customer;
    }

    public async Task<CustomerProfileDalDto?> FindProfileAsync(
        Guid customerId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _dbContext.Customers
            .AsNoTracking()
            .Where(c => c.Id == customerId && c.ManagementCompanyId == managementCompanyId)
            .Select(c => new CustomerProfileDalDto
            {
                Id = c.Id,
                ManagementCompanyId = c.ManagementCompanyId,
                CompanySlug = c.ManagementCompany!.Slug,
                CompanyName = c.ManagementCompany.Name,
                Name = c.Name,
                Slug = c.Slug,
                RegistryCode = c.RegistryCode,
                BillingEmail = c.BillingEmail,
                BillingAddress = c.BillingAddress,
                Phone = c.Phone,
                IsActive = c.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        return customer;
    }

    public async Task<bool> RegistryCodeExistsInCompanyAsync(
        Guid managementCompanyId,
        string registryCode,
        Guid? exceptCustomerId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedRegistryCode = registryCode.Trim().ToLower();

        return await _dbContext.Customers
            .AsNoTracking()
            .Where(c => c.ManagementCompanyId == managementCompanyId)
            .Where(c => exceptCustomerId == null || c.Id != exceptCustomerId.Value)
            .AnyAsync(c => c.RegistryCode.ToLower() == normalizedRegistryCode, cancellationToken);
    }

    public async Task<string?> FindActiveManagementCompanyRoleCodeAsync(
        Guid managementCompanyId,
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Where(mcu => mcu.ManagementCompanyId == managementCompanyId && mcu.AppUserId == appUserId)
            .Where(mcu => mcu.IsActive)
            .Where(mcu => mcu.ValidFrom <= today)
            .Where(mcu => !mcu.ValidTo.HasValue || mcu.ValidTo.Value >= today)
            .Select(mcu => mcu.ManagementCompanyRole!.Code)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateProfileAsync(
        CustomerUpdateDalDto dto,
        CancellationToken cancellationToken = default)
    {
        var customer = await _dbContext.Customers
            .AsTracking()
            .FirstOrDefaultAsync(
                c => c.Id == dto.Id && c.ManagementCompanyId == dto.ManagementCompanyId,
                cancellationToken);

        if (customer is null)
        {
            return;
        }

        customer.Name = dto.Name;
        customer.RegistryCode = dto.RegistryCode;
        customer.BillingEmail = dto.BillingEmail;
        customer.BillingAddress = dto.BillingAddress;
        customer.Phone = dto.Phone;
        customer.IsActive = dto.IsActive;
    }

    public async Task<bool> DeleteAsync(
        Guid customerId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _dbContext.Customers
            .AsNoTracking()
            .Where(c => c.Id == customerId && c.ManagementCompanyId == managementCompanyId)
            .Select(c => new { c.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (customer is null)
        {
            return false;
        }

        var propertyIds = await _dbContext.Properties
            .Where(p => p.CustomerId == customer.Id)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var unitIds = await _dbContext.Units
            .Where(u => propertyIds.Contains(u.PropertyId))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var ticketIds = await _dbContext.Tickets
            .Where(t => (t.CustomerId.HasValue && t.CustomerId.Value == customer.Id)
                        || (t.PropertyId.HasValue && propertyIds.Contains(t.PropertyId.Value))
                        || (t.UnitId.HasValue && unitIds.Contains(t.UnitId.Value)))
            .Where(t => t.ManagementCompanyId == managementCompanyId)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        await DeleteTicketsAsync(ticketIds, cancellationToken);

        await _dbContext.CustomerRepresentatives
            .Where(cr => cr.CustomerId == customer.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Leases
            .Where(l => unitIds.Contains(l.UnitId))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Units
            .Where(u => unitIds.Contains(u.Id))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Properties
            .Where(p => propertyIds.Contains(p.Id))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Customers
            .Where(c => c.Id == customer.Id && c.ManagementCompanyId == managementCompanyId)
            .ExecuteDeleteAsync(cancellationToken);

        return true;
    }

    private async Task DeleteTicketsAsync(
        IReadOnlyCollection<Guid> ticketIds,
        CancellationToken cancellationToken)
    {
        if (ticketIds.Count == 0)
        {
            return;
        }

        var scheduledWorkIds = await _dbContext.ScheduledWorks
            .Where(sw => ticketIds.Contains(sw.TicketId))
            .Select(sw => sw.Id)
            .ToListAsync(cancellationToken);

        if (scheduledWorkIds.Count > 0)
        {
            await _dbContext.WorkLogs
                .Where(wl => scheduledWorkIds.Contains(wl.ScheduledWorkId))
                .ExecuteDeleteAsync(cancellationToken);

            await _dbContext.ScheduledWorks
                .Where(sw => scheduledWorkIds.Contains(sw.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await _dbContext.Tickets
            .Where(t => ticketIds.Contains(t.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
