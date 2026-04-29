using App.Contracts.DAL.ManagementCompanies;
using App.Contracts.DAL.Lookups;
using App.DAL.EF.Mappers;
using App.DAL.EF.Mappers.ManagementCompanies;
using App.Domain;
using Base.Domain;
using Base.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public sealed class ManagementCompanyRepository :
    BaseRepository<ManagementCompanyDalDto, ManagementCompany, AppDbContext>,
    IManagementCompanyRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ManagementCompanyDalMapper _mapper;

    public ManagementCompanyRepository(AppDbContext dbContext, ManagementCompanyDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ManagementCompanyDalDto?> FirstBySlugAsync(
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = companySlug.Trim();

        var company = await _dbContext.ManagementCompanies
            .AsNoTracking()
            .Where(c => c.Slug == normalizedSlug)
            .FirstOrDefaultAsync(cancellationToken);

        return _mapper.Map(company);
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
            .FirstOrDefaultAsync(c => c.Slug == normalizedSlug, cancellationToken);

        return _mapper.MapProfile(company);
    }

    public async Task<ManagementCompanyProfileDalDto?> FirstProfileByIdAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var company = await _dbContext.ManagementCompanies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == managementCompanyId, cancellationToken);

        return _mapper.MapProfile(company);
    }

    public async Task<ManagementCompanyDalDto?> FirstActiveByRegistryCodeAsync(
        string registryCode,
        CancellationToken cancellationToken = default)
    {
        var normalized = registryCode.Trim();
        var company = await _dbContext.ManagementCompanies
            .AsNoTracking()
            .Where(company => company.IsActive)
            .SingleOrDefaultAsync(company => company.RegistryCode == normalized, cancellationToken);

        return _mapper.Map(company);
    }

    public async Task<ManagementCompanyMembershipDalDto?> FirstMembershipByUserAndCompanyAsync(
        Guid appUserId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var membership = await MembershipQuery()
            .FirstOrDefaultAsync(
                membership => membership.AppUserId == appUserId
                              && membership.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        return MapMembership(membership);
    }

    public async Task<IReadOnlyList<ManagementCompanyMembershipDalDto>> MembersByCompanyAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var members = await MembershipQuery()
            .Where(membership => membership.ManagementCompanyId == managementCompanyId)
            .OrderBy(membership => membership.AppUser!.LastName)
            .ThenBy(membership => membership.AppUser!.FirstName)
            .ToListAsync(cancellationToken);

        return members.Select(MapMembership).OfType<ManagementCompanyMembershipDalDto>().ToList();
    }

    public async Task<ManagementCompanyMembershipDalDto?> FindMemberByIdAndCompanyAsync(
        Guid membershipId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var membership = await MembershipQuery()
            .FirstOrDefaultAsync(
                membership => membership.Id == membershipId
                              && membership.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        return MapMembership(membership);
    }

    public async Task<IReadOnlyList<ManagementCompanyMembershipDalDto>> FindMembersByIdsAndCompanyAsync(
        Guid managementCompanyId,
        IReadOnlyCollection<Guid> membershipIds,
        CancellationToken cancellationToken = default)
    {
        var members = await MembershipQuery()
            .Where(membership => membership.ManagementCompanyId == managementCompanyId
                                 && membershipIds.Contains(membership.Id))
            .ToListAsync(cancellationToken);

        return members.Select(MapMembership).OfType<ManagementCompanyMembershipDalDto>().ToList();
    }

    public async Task<IReadOnlyList<LookupDalDto>> AllManagementCompanyRolesAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await _dbContext.ManagementCompanyRoles
            .AsNoTracking()
            .OrderBy(role => role.Code)
            .ToListAsync(cancellationToken);

        return roles.Select(LookupDalMapper.Map).ToList();
    }

    public async Task<LookupDalDto?> FindManagementCompanyRoleByIdAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var role = await _dbContext.ManagementCompanyRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(role => role.Id == roleId, cancellationToken);

        return role is null ? null : LookupDalMapper.Map(role);
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

    private IQueryable<ManagementCompanyUser> MembershipQuery()
    {
        return _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Include(membership => membership.AppUser)
            .Include(membership => membership.ManagementCompany)
            .Include(membership => membership.ManagementCompanyRole);
    }

    private static ManagementCompanyMembershipDalDto? MapMembership(ManagementCompanyUser? membership)
    {
        if (membership is null)
        {
            return null;
        }

        return new ManagementCompanyMembershipDalDto
        {
            Id = membership.Id,
            ManagementCompanyId = membership.ManagementCompanyId,
            AppUserId = membership.AppUserId,
            CompanySlug = membership.ManagementCompany?.Slug ?? string.Empty,
            CompanyName = membership.ManagementCompany?.Name ?? string.Empty,
            RoleId = membership.ManagementCompanyRoleId,
            RoleCode = membership.ManagementCompanyRole?.Code ?? string.Empty,
            RoleLabel = membership.ManagementCompanyRole?.Label.ToString() ?? string.Empty,
            FirstName = membership.AppUser?.FirstName ?? string.Empty,
            LastName = membership.AppUser?.LastName ?? string.Empty,
            Email = membership.AppUser?.Email ?? string.Empty,
            JobTitle = membership.JobTitle.ToString(),
            IsActive = membership.IsActive,
            ValidFrom = membership.ValidFrom,
            ValidTo = membership.ValidTo
        };
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
