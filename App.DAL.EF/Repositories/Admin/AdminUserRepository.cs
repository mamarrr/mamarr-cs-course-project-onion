using App.DAL.Contracts.Repositories.Admin;
using App.DAL.DTO.Admin.Users;
using App.DAL.EF.Mappers.Admin;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories.Admin;

public class AdminUserRepository : IAdminUserRepository
{
    private const string SystemAdminRoleName = "SystemAdmin";
    private readonly AppDbContext _dbContext;
    private readonly AdminUserDalMapper _mapper;

    public AdminUserRepository(AppDbContext dbContext, AdminUserDalMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<AdminUserListItemDalDto>> SearchUsersAsync(AdminUserSearchDalDto search, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search.SearchText))
        {
            var term = search.SearchText.Trim().ToUpperInvariant();
            query = query.Where(user =>
                (user.Email != null && user.Email.ToUpper().Contains(term)) ||
                user.FirstName.ToUpper().Contains(term) ||
                user.LastName.ToUpper().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(search.Email))
        {
            var email = search.Email.Trim().ToUpperInvariant();
            query = query.Where(user => user.Email != null && user.Email.ToUpper().Contains(email));
        }

        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            var name = search.Name.Trim().ToUpperInvariant();
            query = query.Where(user => user.FirstName.ToUpper().Contains(name) || user.LastName.ToUpper().Contains(name));
        }

        if (search.LockedOnly)
        {
            query = query.Where(user => user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow);
        }

        if (search.CreatedFrom.HasValue)
        {
            query = query.Where(user => user.CreatedAt >= search.CreatedFrom.Value);
        }

        if (search.CreatedTo.HasValue)
        {
            query = query.Where(user => user.CreatedAt <= search.CreatedTo.Value);
        }

        var systemAdminUserIds = await SystemAdminUserIds(cancellationToken);
        if (search.HasSystemAdminRole)
        {
            query = query.Where(user => systemAdminUserIds.Contains(user.Id));
        }

        var users = await query
            .OrderByDescending(user => user.CreatedAt)
            .ToListAsync(cancellationToken);

        return users.Select(user => _mapper.Map(user, systemAdminUserIds.Contains(user.Id))).ToList();
    }

    public async Task<AdminUserDetailsDalDto?> GetUserDetailsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roles = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId)
            .Join(_dbContext.Roles.AsNoTracking(),
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => new AdminUserRoleDalDto
                {
                    RoleId = role.Id,
                    RoleName = role.Name ?? string.Empty
                })
            .OrderBy(role => role.RoleName)
            .ToListAsync(cancellationToken);

        var memberships = await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Include(membership => membership.ManagementCompany)
            .Include(membership => membership.ManagementCompanyRole)
            .Where(membership => membership.AppUserId == userId)
            .OrderBy(membership => membership.ManagementCompany!.Name)
            .ToListAsync(cancellationToken);

        var listItem = _mapper.Map(user, roles.Any(role => role.RoleName == SystemAdminRoleName));
        return new AdminUserDetailsDalDto
        {
            Id = listItem.Id,
            Email = listItem.Email,
            FullName = listItem.FullName,
            CreatedAt = listItem.CreatedAt,
            LockoutEnd = listItem.LockoutEnd,
            IsLocked = listItem.IsLocked,
            HasSystemAdminRole = listItem.HasSystemAdminRole,
            PhoneNumber = user.PhoneNumber,
            LastLoginAt = user.LastLoginAt,
            RefreshTokenCount = await _dbContext.RefreshTokens.CountAsync(token => token.AppUserId == userId, cancellationToken),
            Roles = roles,
            CompanyMemberships = memberships.Select(_mapper.Map).ToList()
        };
    }

    public Task<bool> HasSystemAdminRoleAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId)
            .Join(_dbContext.Roles.AsNoTracking(),
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => role.Name)
            .AnyAsync(roleName => roleName == SystemAdminRoleName, cancellationToken);
    }

    public async Task<int> CountSystemAdminsAsync(CancellationToken cancellationToken = default)
    {
        var roleId = await _dbContext.Roles
            .AsNoTracking()
            .Where(role => role.Name == SystemAdminRoleName)
            .Select(role => role.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (roleId == Guid.Empty)
        {
            return 0;
        }

        return await _dbContext.UserRoles.CountAsync(userRole => userRole.RoleId == roleId, cancellationToken);
    }

    public async Task<bool> SetLockoutEndAsync(Guid userId, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.LockoutEnabled = true;
        user.LockoutEnd = lockoutEnd;
        return true;
    }

    private async Task<List<Guid>> SystemAdminUserIds(CancellationToken cancellationToken)
    {
        return await _dbContext.UserRoles
            .AsNoTracking()
            .Join(_dbContext.Roles.AsNoTracking().Where(role => role.Name == SystemAdminRoleName),
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => userRole.UserId)
            .ToListAsync(cancellationToken);
    }
}
