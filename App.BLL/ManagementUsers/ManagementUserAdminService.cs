using App.DAL.EF;
using App.Domain;
using App.BLL.Onboarding;
using App.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.BLL.ManagementUsers;

/// <summary>
/// Implementation of management user administration service with tenant isolation and role checks.
/// </summary>
public class ManagementUserAdminService : IManagementUserAdminService
{
    private readonly AppDbContext _dbContext;
    private readonly IManagementCompanyJoinRequestService _joinRequestService;

    // Role codes that are allowed to administer company users
    private static readonly HashSet<string> AdminRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER"
    };

    public ManagementUserAdminService(
        AppDbContext dbContext,
        IManagementCompanyJoinRequestService joinRequestService,
        ILogger<ManagementUserAdminService> logger)
    {
        _dbContext = dbContext;
        _joinRequestService = joinRequestService;
    }

    /// <inheritdoc />
    public async Task<ManagementUserAdminAuthorizationResult> AuthorizeAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = companySlug.Trim();
        if (string.IsNullOrEmpty(normalizedSlug))
        {
            return new ManagementUserAdminAuthorizationResult
            {
                CompanyNotFound = true,
                ErrorMessage = "Company slug is required."
            };
        }

        // Resolve company by slug
        var company = await _dbContext.ManagementCompanies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == normalizedSlug, cancellationToken);

        if (company == null)
        {
            return new ManagementUserAdminAuthorizationResult
            {
                CompanyNotFound = true,
                ErrorMessage = "Company not found."
            };
        }

        // Resolve actor's membership in this company with role
        var actorMembership = await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Include(m => m.ManagementCompanyRole)
            .Where(m => m.AppUserId == appUserId
                        && m.ManagementCompanyId == company.Id
                        && m.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (actorMembership == null)
        {
            return new ManagementUserAdminAuthorizationResult
            {
                IsForbidden = true,
                ErrorMessage = "You do not have access to this company."
            };
        }

        var roleCode = actorMembership.ManagementCompanyRole?.Code ?? string.Empty;

        // Check if role is OWNER or MANAGER
        if (!AdminRoleCodes.Contains(roleCode))
        {
            return new ManagementUserAdminAuthorizationResult
            {
                IsForbidden = true,
                ErrorMessage = "You do not have permission to manage company users."
            };
        }

        return new ManagementUserAdminAuthorizationResult
        {
            IsAuthorized = true,
            Context = new ManagementUserAdminAuthorizedContext
            {
                AppUserId = appUserId,
                ManagementCompanyId = company.Id,
                CompanySlug = company.Slug,
                CompanyName = company.Name,
                ActorMembershipId = actorMembership.Id,
                ActorRoleCode = roleCode
            }
        };
    }

    /// <inheritdoc />
    public async Task<ManagementUserListResult> ListCompanyMembersAsync(
        ManagementUserAdminAuthorizedContext context,
        CancellationToken cancellationToken = default)
    {
        var members = await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Where(m => m.ManagementCompanyId == context.ManagementCompanyId)
            .Include(m => m.AppUser)
            .Include(m => m.ManagementCompanyRole)
            .OrderBy(m => m.AppUser!.LastName)
            .ThenBy(m => m.AppUser!.FirstName)
            .Select(m => new ManagementUserListItem
            {
                MembershipId = m.Id,
                AppUserId = m.AppUserId,
                FullName = $"{m.AppUser!.FirstName} {m.AppUser.LastName}",
                Email = m.AppUser.Email ?? string.Empty,
                RoleId = m.ManagementCompanyRoleId,
                RoleCode = m.ManagementCompanyRole!.Code,
                RoleLabel = m.ManagementCompanyRole.Label.ToString(),
                JobTitle = m.JobTitle,
                IsActive = m.IsActive,
                ValidFrom = m.ValidFrom,
                ValidTo = m.ValidTo,
                IsActor = m.Id == context.ActorMembershipId
            })
            .ToListAsync(cancellationToken);

        return new ManagementUserListResult
        {
            Members = members
        };
    }

    /// <inheritdoc />
    public async Task<ManagementUserEditResult> GetMembershipForEditAsync(
        ManagementUserAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default)
    {
        var membership = await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Where(m => m.Id == membershipId && m.ManagementCompanyId == context.ManagementCompanyId)
            .Include(m => m.AppUser)
            .Include(m => m.ManagementCompanyRole)
            .Select(m => new ManagementUserEditModel
            {
                MembershipId = m.Id,
                AppUserId = m.AppUserId,
                FullName = $"{m.AppUser!.FirstName} {m.AppUser.LastName}",
                Email = m.AppUser.Email ?? string.Empty,
                RoleId = m.ManagementCompanyRoleId,
                JobTitle = m.JobTitle,
                IsActive = m.IsActive,
                ValidFrom = m.ValidFrom,
                ValidTo = m.ValidTo
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (membership == null)
        {
            return new ManagementUserEditResult
            {
                NotFound = true,
                ErrorMessage = "Membership not found."
            };
        }

        return new ManagementUserEditResult
        {
            Success = true,
            Data = membership
        };
    }

    /// <inheritdoc />
    public async Task<ManagementUserAddResult> AddUserByEmailAsync(
        ManagementUserAdminAuthorizedContext context,
        ManagementUserAddRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Find existing app user by email
        var appUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (appUser == null)
        {
            return new ManagementUserAddResult
            {
                UserNotFound = true,
                ErrorMessage = "User with this email does not exist. They must register first."
            };
        }

        // Check if membership already exists for this company
        var existingMembership = await _dbContext.ManagementCompanyUsers
            .FirstOrDefaultAsync(m => m.AppUserId == appUser.Id
                                      && m.ManagementCompanyId == context.ManagementCompanyId,
                cancellationToken);

        if (existingMembership != null)
        {
            return new ManagementUserAddResult
            {
                DuplicateMembership = true,
                ErrorMessage = "This user is already a member of this company."
            };
        }

        // Validate role exists
        var roleExists = await _dbContext.ManagementCompanyRoles
            .AnyAsync(r => r.Id == request.RoleId, cancellationToken);

        if (!roleExists)
        {
            return new ManagementUserAddResult
            {
                InvalidRole = true,
                ErrorMessage = "Selected role is invalid."
            };
        }

        // Create new membership
        var membership = new ManagementCompanyUser
        {
            Id = Guid.NewGuid(),
            ManagementCompanyId = context.ManagementCompanyId,
            AppUserId = appUser.Id,
            ManagementCompanyRoleId = request.RoleId,
            JobTitle = request.JobTitle.Trim(),
            IsActive = request.IsActive,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ManagementCompanyUsers.Add(membership);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ManagementUserAddResult
        {
            Success = true,
            CreatedMembershipId = membership.Id
        };
    }

    /// <inheritdoc />
    public async Task<ManagementUserUpdateResult> UpdateMembershipAsync(
        ManagementUserAdminAuthorizedContext context,
        Guid membershipId,
        ManagementUserUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        // Find membership scoped to current company
        var membership = await _dbContext.ManagementCompanyUsers
            .AsTracking()
            .Include(m => m.ManagementCompanyRole)
            .FirstOrDefaultAsync(m => m.Id == membershipId
                                      && m.ManagementCompanyId == context.ManagementCompanyId,
                cancellationToken);

        if (membership == null)
        {
            return new ManagementUserUpdateResult
            {
                NotFound = true,
                ErrorMessage = "Membership not found."
            };
        }

        // Validate role exists
        var roleExists = await _dbContext.ManagementCompanyRoles
            .AnyAsync(r => r.Id == request.RoleId, cancellationToken);

        if (!roleExists)
        {
            return new ManagementUserUpdateResult
            {
                InvalidRole = true,
                ErrorMessage = "Selected role is invalid."
            };
        }

        // TODO: Guardrail - prevent self-demotion or removal that leaves company without manager
        // This is noted as a follow-up item per implementation plan

        membership.ManagementCompanyRoleId = request.RoleId;
        membership.JobTitle = request.JobTitle.Trim();
        membership.IsActive = request.IsActive;
        membership.ValidFrom = request.ValidFrom;
        membership.ValidTo = request.ValidTo;

        var affectedRows = await _dbContext.SaveChangesAsync(cancellationToken);

        if (affectedRows == 0)
        {
            return new ManagementUserUpdateResult
            {
                Success = false,
                ErrorMessage = "No changes were saved. Please retry."
            };
        }

        return new ManagementUserUpdateResult
        {
            Success = true
        };
    }

    /// <inheritdoc />
    public async Task<ManagementUserDeleteResult> DeleteMembershipAsync(
        ManagementUserAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default)
    {
        // Find membership scoped to current company
        var membership = await _dbContext.ManagementCompanyUsers
            .Include(m => m.ManagementCompanyRole)
            .FirstOrDefaultAsync(m => m.Id == membershipId
                                      && m.ManagementCompanyId == context.ManagementCompanyId,
                cancellationToken);

        if (membership == null)
        {
            return new ManagementUserDeleteResult
            {
                NotFound = true,
                ErrorMessage = "Membership not found."
            };
        }

        // Prevent deleting own membership through this action
        if (membership.Id == context.ActorMembershipId)
        {
            return new ManagementUserDeleteResult
            {
                Forbidden = true,
                ErrorMessage = "You cannot remove your own membership."
            };
        }

        // TODO: Guardrail - prevent deletion of last OWNER if such role exists
        // This is noted as a follow-up item per implementation plan

        // Hard delete the membership (removes company access only, not the AppUser)
        _dbContext.ManagementCompanyUsers.Remove(membership);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ManagementUserDeleteResult
        {
            Success = true
        };
    }

    /// <inheritdoc />
    public async Task<PendingAccessRequestListResult> GetPendingAccessRequestsAsync(
        ManagementUserAdminAuthorizedContext context,
        CancellationToken cancellationToken = default)
    {
        var requests = await _joinRequestService.ListPendingForCompanyAsync(context.ManagementCompanyId, cancellationToken);

        return new PendingAccessRequestListResult
        {
            Requests = requests.Select(x => new PendingAccessRequestItem
            {
                RequestId = x.RequestId,
                AppUserId = x.AppUserId,
                RequesterName = x.RequesterName,
                RequesterEmail = x.RequesterEmail,
                RequestedRoleCode = x.RequestedRoleCode,
                RequestedRoleLabel = x.RequestedRoleLabel,
                Message = x.Message,
                RequestedAt = x.CreatedAt
            }).ToList()
        };
    }

    /// <inheritdoc />
    public async Task<PendingAccessRequestActionResult> ApprovePendingAccessRequestAsync(
        ManagementUserAdminAuthorizedContext context,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var result = await _joinRequestService.ApproveRequestAsync(
            context.AppUserId,
            context.ManagementCompanyId,
            requestId,
            cancellationToken);

        return new PendingAccessRequestActionResult
        {
            Success = result.Success,
            NotFound = result.NotFound,
            Forbidden = result.Forbidden,
            AlreadyResolved = result.AlreadyResolved,
            AlreadyMember = result.AlreadyMember,
            ErrorMessage = result.ErrorMessage
        };
    }

    /// <inheritdoc />
    public async Task<PendingAccessRequestActionResult> RejectPendingAccessRequestAsync(
        ManagementUserAdminAuthorizedContext context,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var result = await _joinRequestService.RejectRequestAsync(
            context.AppUserId,
            context.ManagementCompanyId,
            requestId,
            cancellationToken);

        return new PendingAccessRequestActionResult
        {
            Success = result.Success,
            NotFound = result.NotFound,
            Forbidden = result.Forbidden,
            AlreadyResolved = result.AlreadyResolved,
            AlreadyMember = result.AlreadyMember,
            ErrorMessage = result.ErrorMessage
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ManagementCompanyRole>> GetAvailableRolesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ManagementCompanyRoles
            .AsNoTracking()
            .OrderBy(r => r.Code)
            .ToListAsync(cancellationToken);
    }
}
