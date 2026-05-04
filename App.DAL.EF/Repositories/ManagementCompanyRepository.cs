using App.DAL.Contracts.DAL.ManagementCompanies;
using App.DAL.Contracts.DAL.Lookups;
using App.DAL.EF.Mappers.ManagementCompanies;
using App.Domain;
using Base.Domain;
using Base.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class ManagementCompanyRepository :
    BaseRepository<ManagementCompanyDalDto, ManagementCompany, AppDbContext>,
    IManagementCompanyRepository
{
    private readonly AppDbContext _dbContext;

    public ManagementCompanyRepository(AppDbContext dbContext, ManagementCompanyDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
    }

    public async Task<ManagementCompanyDalDto?> FirstBySlugAsync(
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = companySlug.Trim();

        return await _dbContext.ManagementCompanies
            .AsNoTracking()
            .Where(c => c.Slug == normalizedSlug)
            .Select(c => new ManagementCompanyDalDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                IsActive = c.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<string?> FindActiveUserRoleCodeAsync(
        Guid appUserId,
        Guid managementCompanyId,
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

    public async Task<ManagementCompanyProfileDalDto?> FirstProfileBySlugAsync(
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = companySlug.Trim();

        var company = await _dbContext.ManagementCompanies
            .AsNoTracking()
            .Where(c => c.Slug == normalizedSlug)
            .Select(c => new ManagementCompanyProfileDalDto
            {
                Id = c.Id,
                Slug = c.Slug,
                Name = c.Name,
                RegistryCode = c.RegistryCode,
                VatNumber = c.VatNumber,
                Email = c.Email,
                Phone = c.Phone,
                Address = c.Address,
                IsActive = c.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        return company;
    }

    public async Task<ManagementCompanyProfileDalDto?> FirstProfileByIdAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var company = await _dbContext.ManagementCompanies
            .AsNoTracking()
            .Where(c => c.Id == managementCompanyId)
            .Select(c => new ManagementCompanyProfileDalDto
            {
                Id = c.Id,
                Slug = c.Slug,
                Name = c.Name,
                RegistryCode = c.RegistryCode,
                VatNumber = c.VatNumber,
                Email = c.Email,
                Phone = c.Phone,
                Address = c.Address,
                IsActive = c.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        return company;
    }

    public async Task<ManagementCompanyDalDto?> FirstActiveByRegistryCodeAsync(
        string registryCode,
        CancellationToken cancellationToken = default)
    {
        var normalized = registryCode.Trim();
        return await _dbContext.ManagementCompanies
            .AsNoTracking()
            .Where(company => company.IsActive)
            .Where(company => company.RegistryCode == normalized)
            .Select(company => new ManagementCompanyDalDto
            {
                Id = company.Id,
                Name = company.Name,
                Slug = company.Slug,
                IsActive = company.IsActive
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> RegistryCodeExistsAsync(
        string registryCode,
        CancellationToken cancellationToken = default)
    {
        var normalized = registryCode.Trim();

        return await _dbContext.ManagementCompanies
            .AsNoTracking()
            .AnyAsync(company => company.RegistryCode == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> AllSlugsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ManagementCompanies
            .AsNoTracking()
            .Select(company => company.Slug)
            .ToListAsync(cancellationToken);
    }

    public Task<ManagementCompanyDalDto> AddManagementCompanyAsync(
        ManagementCompanyCreateDalDto dto,
        CancellationToken cancellationToken = default)
    {
        var company = new ManagementCompany
        {
            Id = dto.Id,
            Name = dto.Name,
            Slug = dto.Slug,
            RegistryCode = dto.RegistryCode,
            VatNumber = dto.VatNumber,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            CreatedAt = dto.CreatedAt,
            IsActive = dto.IsActive
        };

        _dbContext.ManagementCompanies.Add(company);
        return Task.FromResult(new ManagementCompanyDalDto
        {
            Id = company.Id,
            Name = company.Name,
            Slug = company.Slug,
            IsActive = company.IsActive
        });
    }

    public async Task<IReadOnlyList<ManagementCompanyContextDalDto>> ActiveUserManagementContextsAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Where(membership => membership.AppUserId == appUserId)
            .Where(membership => membership.IsActive)
            .Where(membership => membership.ValidFrom <= today)
            .Where(membership => !membership.ValidTo.HasValue || membership.ValidTo.Value >= today)
            .Where(membership => membership.ManagementCompany != null && membership.ManagementCompany.IsActive)
            .OrderBy(membership => membership.ManagementCompany!.Name)
            .Select(membership => new ManagementCompanyContextDalDto
            {
                ManagementCompanyId = membership.ManagementCompanyId,
                Slug = membership.ManagementCompany!.Slug,
                CompanyName = membership.ManagementCompany.Name,
                MembershipId = membership.Id,
                RoleId = membership.ManagementCompanyRoleId,
                RoleCode = membership.ManagementCompanyRole!.Code,
                IsActive = membership.IsActive,
                ValidFrom = membership.ValidFrom,
                ValidTo = membership.ValidTo
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ManagementCompanyContextDalDto?> ActiveUserManagementContextByCompanyIdAsync(
        Guid appUserId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Where(membership => membership.AppUserId == appUserId)
            .Where(membership => membership.ManagementCompanyId == managementCompanyId)
            .Where(membership => membership.IsActive)
            .Where(membership => membership.ValidFrom <= today)
            .Where(membership => !membership.ValidTo.HasValue || membership.ValidTo.Value >= today)
            .Where(membership => membership.ManagementCompany != null && membership.ManagementCompany.IsActive)
            .Select(membership => new ManagementCompanyContextDalDto
            {
                ManagementCompanyId = membership.ManagementCompanyId,
                Slug = membership.ManagementCompany!.Slug,
                CompanyName = membership.ManagementCompany.Name,
                MembershipId = membership.Id,
                RoleId = membership.ManagementCompanyRoleId,
                RoleCode = membership.ManagementCompanyRole!.Code,
                IsActive = membership.IsActive,
                ValidFrom = membership.ValidFrom,
                ValidTo = membership.ValidTo
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ActiveUserManagementContextExistsBySlugAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = companySlug.Trim();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Where(membership => membership.AppUserId == appUserId)
            .Where(membership => membership.IsActive)
            .Where(membership => membership.ValidFrom <= today)
            .Where(membership => !membership.ValidTo.HasValue || membership.ValidTo.Value >= today)
            .AnyAsync(
                membership => membership.ManagementCompany != null
                              && membership.ManagementCompany.IsActive
                              && membership.ManagementCompany.Slug == normalizedSlug,
                cancellationToken);
    }

    public async Task<ManagementCompanyMembershipDalDto?> FirstMembershipByUserAndCompanyAsync(
        Guid appUserId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await MembershipQuery()
            .FirstOrDefaultAsync(
                membership => membership.AppUserId == appUserId
                              && membership.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ManagementCompanyMembershipDalDto>> MembersByCompanyAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await MembershipQuery()
            .Where(membership => membership.ManagementCompanyId == managementCompanyId)
            .OrderBy(membership => membership.LastName)
            .ThenBy(membership => membership.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<ManagementCompanyMembershipDalDto?> FindMemberByIdAndCompanyAsync(
        Guid membershipId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await MembershipQuery()
            .FirstOrDefaultAsync(
                membership => membership.Id == membershipId
                              && membership.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ManagementCompanyMembershipDalDto>> FindMembersByIdsAndCompanyAsync(
        Guid managementCompanyId,
        IReadOnlyCollection<Guid> membershipIds,
        CancellationToken cancellationToken = default)
    {
        return await MembershipQuery()
            .Where(membership => membership.ManagementCompanyId == managementCompanyId
                                 && membershipIds.Contains(membership.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupDalDto>> AllManagementCompanyRolesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ManagementCompanyRoles
            .AsNoTracking()
            .OrderBy(role => role.Code)
            .Select(role => new LookupDalDto
            {
                Id = role.Id,
                Code = role.Code,
                Label = role.Label.ToString()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<LookupDalDto?> FindManagementCompanyRoleByIdAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ManagementCompanyRoles
            .AsNoTracking()
            .Select(role => new LookupDalDto
            {
                Id = role.Id,
                Code = role.Code,
                Label = role.Label.ToString()
            })
            .FirstOrDefaultAsync(role => role.Id == roleId, cancellationToken);
    }

    public async Task<Guid?> FindAppUserIdByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Email != null && user.Email.ToLower() == normalizedEmail)
            .Select(user => (Guid?)user.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> UserBelongsToCompanyAsync(
        Guid appUserId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Where(mcu => mcu.ManagementCompanyId == managementCompanyId && mcu.AppUserId == appUserId)
            .Where(mcu => mcu.IsActive)
            .Where(mcu => mcu.ValidFrom <= today)
            .Where(mcu => !mcu.ValidTo.HasValue || mcu.ValidTo.Value >= today)
            .AnyAsync(cancellationToken);
    }

    public async Task<bool> MembershipExistsAsync(
        Guid appUserId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .AnyAsync(
                membership => membership.AppUserId == appUserId
                              && membership.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }

    public async Task<bool> RegistryCodeExistsOutsideCompanyAsync(
        Guid managementCompanyId,
        string normalizedRegistryCode,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ManagementCompanies
            .AsNoTracking()
            .AnyAsync(
                company => company.Id != managementCompanyId
                           && company.RegistryCode.ToLower() == normalizedRegistryCode.ToLower(),
                cancellationToken);
    }

    public void AddMembership(ManagementCompanyMembershipCreateDalDto dto)
    {
        _dbContext.ManagementCompanyUsers.Add(new ManagementCompanyUser
        {
            Id = dto.Id,
            ManagementCompanyId = dto.ManagementCompanyId,
            AppUserId = dto.AppUserId,
            ManagementCompanyRoleId = dto.RoleId,
            JobTitle = new LangStr(dto.JobTitle),
            IsActive = dto.IsActive,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            CreatedAt = dto.CreatedAt
        });
    }

    public async Task<bool> ApplyMembershipUpdateAsync(
        ManagementCompanyMembershipUpdateDalDto dto,
        CancellationToken cancellationToken = default)
    {
        var membership = await _dbContext.ManagementCompanyUsers
            .AsTracking()
            .FirstOrDefaultAsync(
                membership => membership.Id == dto.MembershipId
                              && membership.ManagementCompanyId == dto.ManagementCompanyId,
                cancellationToken);

        if (membership is null)
        {
            return false;
        }

        membership.ManagementCompanyRoleId = dto.RoleId;
        if (membership.JobTitle.ToString() != dto.JobTitle)
        {
            membership.JobTitle.SetTranslation(dto.JobTitle);
            _dbContext.Entry(membership).Property(nameof(ManagementCompanyUser.JobTitle)).IsModified = true;
        }

        membership.IsActive = dto.IsActive;
        membership.ValidFrom = dto.ValidFrom;
        membership.ValidTo = dto.ValidTo;
        return true;
    }

    public async Task<bool> RemoveMembershipAsync(
        Guid membershipId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var membership = await _dbContext.ManagementCompanyUsers
            .FirstOrDefaultAsync(
                membership => membership.Id == membershipId
                              && membership.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (membership is null)
        {
            return false;
        }

        _dbContext.ManagementCompanyUsers.Remove(membership);
        return true;
    }

    public async Task<bool> UpdateProfileAsync(
        ManagementCompanyProfileUpdateDalDto dto,
        CancellationToken cancellationToken = default)
    {
        var company = await _dbContext.ManagementCompanies
            .AsTracking()
            .FirstOrDefaultAsync(company => company.Id == dto.Id, cancellationToken);

        if (company is null)
        {
            return false;
        }

        company.Name = dto.Name;
        company.RegistryCode = dto.RegistryCode;
        company.VatNumber = dto.VatNumber;
        company.Email = dto.Email;
        company.Phone = dto.Phone;
        company.Address = dto.Address;
        company.IsActive = dto.IsActive;
        return true;
    }

    public async Task<bool> SetMembershipRoleAsync(
        Guid membershipId,
        Guid managementCompanyId,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var membership = await _dbContext.ManagementCompanyUsers
            .AsTracking()
            .FirstOrDefaultAsync(
                membership => membership.Id == membershipId
                              && membership.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (membership is null)
        {
            return false;
        }

        membership.ManagementCompanyRoleId = roleId;
        return true;
    }

    public async Task<int> CountEffectiveOwnersAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var ownerRoleIds = await _dbContext.ManagementCompanyRoles
            .AsNoTracking()
            .Where(role => role.Code == "OWNER")
            .Select(role => role.Id)
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Where(membership => membership.ManagementCompanyId == managementCompanyId
                                 && ownerRoleIds.Contains(membership.ManagementCompanyRoleId)
                                 && membership.IsActive
                                 && membership.ValidFrom <= today
                                 && (!membership.ValidTo.HasValue || membership.ValidTo >= today))
            .CountAsync(cancellationToken);
    }

    public async Task<bool> DeleteCascadeAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var company = await _dbContext.ManagementCompanies
            .AsNoTracking()
            .Where(mc => mc.Id == managementCompanyId)
            .Select(mc => new { mc.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (company is null)
        {
            return false;
        }

        var customerIds = await _dbContext.Customers
            .Where(c => c.ManagementCompanyId == company.Id)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var propertyIds = await _dbContext.Properties
            .Where(p => customerIds.Contains(p.CustomerId))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var unitIds = await _dbContext.Units
            .Where(u => propertyIds.Contains(u.PropertyId))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var residentIds = await _dbContext.Residents
            .Where(r => r.ManagementCompanyId == company.Id)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var vendorIds = await _dbContext.Vendors
            .Where(v => v.ManagementCompanyId == company.Id)
            .Select(v => v.Id)
            .ToListAsync(cancellationToken);

        var ticketIds = await _dbContext.Tickets
            .Where(t => t.ManagementCompanyId == company.Id
                        || (t.CustomerId.HasValue && customerIds.Contains(t.CustomerId.Value))
                        || (t.PropertyId.HasValue && propertyIds.Contains(t.PropertyId.Value))
                        || (t.UnitId.HasValue && unitIds.Contains(t.UnitId.Value))
                        || (t.ResidentId.HasValue && residentIds.Contains(t.ResidentId.Value))
                        || (t.VendorId.HasValue && vendorIds.Contains(t.VendorId.Value)))
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        await DeleteTicketsAsync(ticketIds, cancellationToken);

        await _dbContext.CustomerRepresentatives
            .Where(cr => customerIds.Contains(cr.CustomerId) || residentIds.Contains(cr.ResidentId))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Leases
            .Where(l => unitIds.Contains(l.UnitId) || residentIds.Contains(l.ResidentId))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.ResidentUsers
            .Where(ru => residentIds.Contains(ru.ResidentId))
            .ExecuteDeleteAsync(cancellationToken);

        var residentContactIds = await _dbContext.ResidentContacts
            .Where(rc => residentIds.Contains(rc.ResidentId))
            .Select(rc => rc.ContactId)
            .ToListAsync(cancellationToken);

        var vendorContactIds = await _dbContext.VendorContacts
            .Where(vc => vendorIds.Contains(vc.VendorId))
            .Select(vc => vc.ContactId)
            .ToListAsync(cancellationToken);

        await _dbContext.ResidentContacts
            .Where(rc => residentIds.Contains(rc.ResidentId))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.VendorContacts
            .Where(vc => vendorIds.Contains(vc.VendorId))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.VendorTicketCategories
            .Where(vtc => vendorIds.Contains(vtc.VendorId))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Vendors
            .Where(v => vendorIds.Contains(v.Id))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Units
            .Where(u => unitIds.Contains(u.Id))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Properties
            .Where(p => propertyIds.Contains(p.Id))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Residents
            .Where(r => residentIds.Contains(r.Id))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Customers
            .Where(c => customerIds.Contains(c.Id))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.ManagementCompanyJoinRequests
            .Where(jr => jr.ManagementCompanyId == company.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.ManagementCompanyUsers
            .Where(mcu => mcu.ManagementCompanyId == company.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var companyContactIds = await _dbContext.Contacts
            .Where(c => c.ManagementCompanyId == company.Id)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var allContactIds = residentContactIds
            .Concat(vendorContactIds)
            .Concat(companyContactIds)
            .Distinct()
            .ToArray();

        await DeleteContactsIfOrphanedAsync(allContactIds, cancellationToken);

        await _dbContext.ManagementCompanies
            .Where(mc => mc.Id == company.Id)
            .ExecuteDeleteAsync(cancellationToken);

        return true;
    }

    private IQueryable<ManagementCompanyMembershipDalDto> MembershipQuery()
    {
        return _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Include(membership => membership.AppUser)
            .Include(membership => membership.ManagementCompany)
            .Include(membership => membership.ManagementCompanyRole)
            .Select(membership => new ManagementCompanyMembershipDalDto
            {
                Id = membership.Id,
                ManagementCompanyId = membership.ManagementCompanyId,
                AppUserId = membership.AppUserId,
                CompanySlug = membership.ManagementCompany == null ? string.Empty : membership.ManagementCompany.Slug,
                CompanyName = membership.ManagementCompany == null ? string.Empty : membership.ManagementCompany.Name,
                RoleId = membership.ManagementCompanyRoleId,
                RoleCode = membership.ManagementCompanyRole == null ? string.Empty : membership.ManagementCompanyRole.Code,
                RoleLabel = membership.ManagementCompanyRole == null ? string.Empty : membership.ManagementCompanyRole.Label.ToString(),
                FirstName = membership.AppUser == null ? string.Empty : membership.AppUser.FirstName,
                LastName = membership.AppUser == null ? string.Empty : membership.AppUser.LastName,
                Email = membership.AppUser == null ? string.Empty : membership.AppUser.Email ?? string.Empty,
                JobTitle = membership.JobTitle.ToString(),
                IsActive = membership.IsActive,
                ValidFrom = membership.ValidFrom,
                ValidTo = membership.ValidTo
            });
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

    private async Task DeleteContactsIfOrphanedAsync(
        IReadOnlyCollection<Guid> contactIds,
        CancellationToken cancellationToken)
    {
        if (contactIds.Count == 0)
        {
            return;
        }

        var orphanedContactIds = await _dbContext.Contacts
            .Where(c => contactIds.Contains(c.Id))
            .Where(c => !_dbContext.ResidentContacts.Any(rc => rc.ContactId == c.Id))
            .Where(c => !_dbContext.VendorContacts.Any(vc => vc.ContactId == c.Id))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        if (orphanedContactIds.Count == 0)
        {
            return;
        }

        await _dbContext.Contacts
            .Where(c => orphanedContactIds.Contains(c.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
