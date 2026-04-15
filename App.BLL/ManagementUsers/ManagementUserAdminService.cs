using App.BLL.Onboarding;
using App.DAL.EF;
using App.Domain;
using App.Domain.Identity;
using Base.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

namespace App.BLL.ManagementUsers;

/// <summary>
/// Implementation of management user administration service with tenant isolation and role checks.
/// </summary>
public class ManagementUserAdminService : IManagementUserAdminService
{
    private const string OwnerRoleCode = "OWNER";
    private const string ManagerRoleCode = "MANAGER";

    private readonly AppDbContext _dbContext;
    private readonly IManagementCompanyJoinRequestService _joinRequestService;

    private static readonly HashSet<string> AdminRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        OwnerRoleCode,
        ManagerRoleCode
    };

    private static readonly HashSet<string> ManagementAreaRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        OwnerRoleCode,
        ManagerRoleCode,
        "FINANCE",
        "SUPPORT"
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
    public async Task<ManagementAreaAuthorizationResult> AuthorizeManagementAreaAccessAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var resolution = await ResolveMembershipContextAsync(appUserId, companySlug, cancellationToken);
        if (resolution.Result != null)
        {
            return resolution.Result;
        }

        if (!ManagementAreaRoleCodes.Contains(resolution.MembershipContext!.ActorRoleCode))
        {
            return new ManagementAreaAuthorizationResult
            {
                IsForbidden = true,
                FailureReason = ManagementAuthorizationFailureReason.InsufficientPrivileges,
                ErrorMessage = "You do not have access to the management area."
            };
        }

        return new ManagementAreaAuthorizationResult
        {
            IsAuthorized = true,
            Context = resolution.MembershipContext
        };
    }

    /// <inheritdoc />
    public async Task<ManagementUserAdminAuthorizationResult> AuthorizeAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var resolution = await ResolveMembershipContextAsync(appUserId, companySlug, cancellationToken);
        if (resolution.Result != null)
        {
            return ConvertToAdminAuthorizationResult(resolution.Result);
        }

        if (!AdminRoleCodes.Contains(resolution.MembershipContext!.ActorRoleCode))
        {
            return new ManagementUserAdminAuthorizationResult
            {
                IsForbidden = true,
                MembershipValidButNotAdmin = true,
                FailureReason = ManagementAuthorizationFailureReason.InsufficientPrivileges,
                ErrorMessage = "You do not have permission to manage company users."
            };
        }

        return new ManagementUserAdminAuthorizationResult
        {
            IsAuthorized = true,
            Context = new ManagementUserAdminAuthorizedContext
            {
                AppUserId = resolution.MembershipContext.AppUserId,
                ManagementCompanyId = resolution.MembershipContext.ManagementCompanyId,
                CompanySlug = resolution.MembershipContext.CompanySlug,
                CompanyName = resolution.MembershipContext.CompanyName,
                ActorMembershipId = resolution.MembershipContext.ActorMembershipId,
                ActorRoleId = resolution.MembershipContext.ActorRoleId,
                ActorRoleCode = resolution.MembershipContext.ActorRoleCode,
                ActorRoleLabel = resolution.MembershipContext.ActorRoleLabel,
                IsOwner = resolution.MembershipContext.IsOwner,
                IsAdmin = true,
                ValidFrom = resolution.MembershipContext.ValidFrom,
                ValidTo = resolution.MembershipContext.ValidTo
            }
        };
    }

    /// <inheritdoc />
    public async Task<ManagementUserListResult> ListCompanyMembersAsync(
        ManagementUserAdminAuthorizedContext context,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var members = await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Where(m => m.ManagementCompanyId == context.ManagementCompanyId)
            .Include(m => m.AppUser)
            .Include(m => m.ManagementCompanyRole)
            .OrderBy(m => m.AppUser!.LastName)
            .ThenBy(m => m.AppUser!.FirstName)
            .ToListAsync(cancellationToken);

        var items = members.Select(m =>
        {
            var roleCode = m.ManagementCompanyRole?.Code ?? string.Empty;
            var isOwner = IsOwnerRole(roleCode);
            var isActor = m.Id == context.ActorMembershipId;
            var isEffective = IsMembershipEffective(m.IsActive, m.ValidFrom, m.ValidTo, today);
            var capabilities = ResolveTargetCapabilities(context, m.Id, roleCode, m.IsActive, m.ValidFrom, m.ValidTo, today);

            return new ManagementUserListItem
            {
                MembershipId = m.Id,
                AppUserId = m.AppUserId,
                FullName = $"{m.AppUser!.FirstName} {m.AppUser.LastName}",
                Email = m.AppUser.Email ?? string.Empty,
                RoleId = m.ManagementCompanyRoleId,
                RoleCode = roleCode,
                RoleLabel = m.ManagementCompanyRole!.Label.ToString(),
                JobTitle = NormalizePossiblySerializedLangStr(m.JobTitle.ToString()),
                IsActive = m.IsActive,
                ValidFrom = m.ValidFrom,
                ValidTo = m.ValidTo,
                IsActor = isActor,
                IsOwner = isOwner,
                IsEffective = isEffective,
                CanEdit = capabilities.CanEdit,
                CanDelete = capabilities.CanDelete,
                CanTransferOwnership = capabilities.CanTransferOwnership,
                CanChangeRole = capabilities.CanChangeRole,
                CanDeactivate = capabilities.CanDeactivate,
                ProtectedReason = capabilities.ProtectedReason,
                ProtectedReasonCode = capabilities.ProtectedReasonCode
            };
        }).ToList();

        return new ManagementUserListResult
        {
            Members = items
        };
    }

    /// <inheritdoc />
    public async Task<ManagementUserEditResult> GetMembershipForEditAsync(
        ManagementUserAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var membership = await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Where(m => m.Id == membershipId && m.ManagementCompanyId == context.ManagementCompanyId)
            .Include(m => m.AppUser)
            .Include(m => m.ManagementCompanyRole)
            .FirstOrDefaultAsync(cancellationToken);

        if (membership == null)
        {
            return new ManagementUserEditResult
            {
                NotFound = true,
                ErrorMessage = "Membership not found."
            };
        }

        var capabilities = ResolveTargetCapabilities(
            context,
            membership.Id,
            membership.ManagementCompanyRole?.Code ?? string.Empty,
            membership.IsActive,
            membership.ValidFrom,
            membership.ValidTo,
            today);

        if (!capabilities.CanEdit && !capabilities.CanTransferOwnership)
        {
            return new ManagementUserEditResult
            {
                Forbidden = true,
                ErrorMessage = capabilities.ProtectedReason ?? "This membership cannot be edited."
            };
        }

        var optionsResult = await GetEditRoleOptionsAsync(context, membershipId, cancellationToken);
        var options = optionsResult.Success ? optionsResult.Options : Array.Empty<ManagementRoleOption>();
        var roleCode = membership.ManagementCompanyRole?.Code ?? string.Empty;

        return new ManagementUserEditResult
        {
            Success = true,
            Data = new ManagementUserEditModel
            {
                MembershipId = membership.Id,
                AppUserId = membership.AppUserId,
                FullName = $"{membership.AppUser!.FirstName} {membership.AppUser.LastName}",
                Email = membership.AppUser.Email ?? string.Empty,
                RoleId = membership.ManagementCompanyRoleId,
                RoleCode = roleCode,
                RoleLabel = membership.ManagementCompanyRole!.Label.ToString(),
                JobTitle = NormalizePossiblySerializedLangStr(membership.JobTitle.ToString()),
                IsActive = membership.IsActive,
                ValidFrom = membership.ValidFrom,
                ValidTo = membership.ValidTo,
                IsOwner = IsOwnerRole(roleCode),
                IsActor = membership.Id == context.ActorMembershipId,
                IsEffective = IsMembershipEffective(membership.IsActive, membership.ValidFrom, membership.ValidTo, today),
                CanEdit = capabilities.CanEdit,
                CanDelete = capabilities.CanDelete,
                CanTransferOwnership = capabilities.CanTransferOwnership,
                CanChangeRole = capabilities.CanChangeRole,
                CanDeactivate = capabilities.CanDeactivate,
                OwnershipTransferRequired = capabilities.ProtectedReasonCode == ManagementUserActionBlockReason.OwnershipTransferRequired,
                ProtectedReason = capabilities.ProtectedReason,
                ProtectedReasonCode = capabilities.ProtectedReasonCode,
                AvailableRoleOptions = options
            }
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ManagementRoleOption>> GetAddRoleOptionsAsync(
        ManagementUserAdminAuthorizedContext context,
        CancellationToken cancellationToken = default)
    {
        var roles = await _dbContext.ManagementCompanyRoles
            .AsNoTracking()
            .OrderBy(r => r.Code)
            .ToListAsync(cancellationToken);

        return roles
            .Where(role => CanAssignRoleInGenericFlow(context, role.Code))
            .Select(MapRoleOption)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<ManagementRoleOptionsResult> GetEditRoleOptionsAsync(
        ManagementUserAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default)
    {
        var membership = await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Where(m => m.Id == membershipId && m.ManagementCompanyId == context.ManagementCompanyId)
            .Include(m => m.ManagementCompanyRole)
            .FirstOrDefaultAsync(cancellationToken);

        if (membership == null)
        {
            return new ManagementRoleOptionsResult
            {
                NotFound = true,
                ErrorMessage = "Membership not found."
            };
        }

        var roleCode = membership.ManagementCompanyRole?.Code ?? string.Empty;
        if (IsOwnerRole(roleCode))
        {
            return new ManagementRoleOptionsResult
            {
                Forbidden = true,
                OwnershipTransferRequired = true,
                ErrorMessage = "Owner role cannot be changed in the standard edit flow. Use ownership transfer instead."
            };
        }

        var roles = await GetAddRoleOptionsAsync(context, cancellationToken);
        return new ManagementRoleOptionsResult
        {
            Success = true,
            Options = roles
        };
    }

    /// <inheritdoc />
    public async Task<ManagementUserAddResult> AddUserByEmailAsync(
        ManagementUserAdminAuthorizedContext context,
        ManagementUserAddRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidDateRange(request.ValidFrom, request.ValidTo))
        {
            return new ManagementUserAddResult
            {
                InvalidDateRange = true,
                ErrorMessage = "Membership validity range is invalid."
            };
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

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

        var role = await _dbContext.ManagementCompanyRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

        if (role == null)
        {
            return new ManagementUserAddResult
            {
                InvalidRole = true,
                ErrorMessage = "Selected role is invalid."
            };
        }

        if (!CanAssignRoleInGenericFlow(context, role.Code))
        {
            return new ManagementUserAddResult
            {
                InvalidRole = true,
                CannotAssignOwner = IsOwnerRole(role.Code),
                ErrorMessage = IsOwnerRole(role.Code)
                    ? "Owner cannot be assigned through the generic add flow."
                    : "Selected role is not allowed for this action."
            };
        }

        var membership = new ManagementCompanyUser
        {
            Id = Guid.NewGuid(),
            ManagementCompanyId = context.ManagementCompanyId,
            AppUserId = appUser.Id,
            ManagementCompanyRoleId = request.RoleId,
            JobTitle = new LangStr(request.JobTitle.Trim()),
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
        if (!IsValidDateRange(request.ValidFrom, request.ValidTo))
        {
            return new ManagementUserUpdateResult
            {
                InvalidDateRange = true,
                BlockReason = ManagementUserActionBlockReason.InvalidDateRange,
                ErrorMessage = "Membership validity range is invalid."
            };
        }

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

        var currentRoleCode = membership.ManagementCompanyRole?.Code ?? string.Empty;
        var role = await _dbContext.ManagementCompanyRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

        if (role == null)
        {
            return new ManagementUserUpdateResult
            {
                InvalidRole = true,
                ErrorMessage = "Selected role is invalid."
            };
        }

        var requestedRoleCode = role.Code;
        var isSelf = membership.Id == context.ActorMembershipId;
        var targetIsOwner = IsOwnerRole(currentRoleCode);

        if (targetIsOwner)
        {

            return new ManagementUserUpdateResult
            {
                Forbidden = true,
                CannotEditOwner = true,
                OwnershipTransferRequired = true,
                BlockReason = ManagementUserActionBlockReason.OwnershipTransferRequired,
                ErrorMessage = "Owner role cannot be changed in the standard edit flow. Use ownership transfer instead."
            };
        }

        if (isSelf && !string.Equals(currentRoleCode, requestedRoleCode, StringComparison.OrdinalIgnoreCase))
        {
            return new ManagementUserUpdateResult
            {
                Forbidden = true,
                CannotChangeOwnRole = true,
                BlockReason = ManagementUserActionBlockReason.SelfProtected,
                ErrorMessage = "You cannot change your own role through the generic edit flow."
            };
        }

        if (isSelf && membership.IsActive && !request.IsActive)
        {
            return new ManagementUserUpdateResult
            {
                Forbidden = true,
                CannotDeactivateSelf = true,
                BlockReason = ManagementUserActionBlockReason.SelfProtected,
                ErrorMessage = "You cannot deactivate your own membership."
            };
        }

        if (IsOwnerRole(requestedRoleCode))
        {
            return new ManagementUserUpdateResult
            {
                Forbidden = true,
                CannotAssignOwner = true,
                BlockReason = ManagementUserActionBlockReason.RoleNotAssignable,
                ErrorMessage = "Owner cannot be assigned through the generic edit flow."
            };
        }

        if (!CanAssignRoleInGenericFlow(context, requestedRoleCode))
        {
            return new ManagementUserUpdateResult
            {
                Forbidden = true,
                InvalidRole = true,
                BlockReason = ManagementUserActionBlockReason.RoleNotAssignable,
                ErrorMessage = "Selected role is not allowed for this action."
            };
        }

        var entry = _dbContext.Entry(membership);
        var jobTitleProperty = entry.Property(nameof(ManagementCompanyUser.JobTitle));
        var roleProperty = entry.Property(nameof(ManagementCompanyUser.ManagementCompanyRoleId));
        var isActiveProperty = entry.Property(nameof(ManagementCompanyUser.IsActive));
        var validFromProperty = entry.Property(nameof(ManagementCompanyUser.ValidFrom));
        var validToProperty = entry.Property(nameof(ManagementCompanyUser.ValidTo));

        membership.ManagementCompanyRoleId = request.RoleId;
        if (membership.JobTitle.ToString() != request.JobTitle.ToString())
        {
            jobTitleProperty.IsModified = true;
            membership.JobTitle.SetTranslation(request.JobTitle);
        }
        membership.IsActive = request.IsActive;
        membership.ValidFrom = request.ValidFrom;
        membership.ValidTo = request.ValidTo;

        var affectedRows = await _dbContext.SaveChangesAsync(cancellationToken);

        if (affectedRows == 0)
        {
            var noTrackedChanges = !roleProperty.IsModified
                                   && !jobTitleProperty.IsModified
                                   && !isActiveProperty.IsModified
                                   && !validFromProperty.IsModified
                                   && !validToProperty.IsModified;

            if (noTrackedChanges)
            {
                return new ManagementUserUpdateResult
                {
                    Success = true
                };
            }

            return new ManagementUserUpdateResult
            {
                Success = false,
                ErrorMessage = App.Resources.Views.UiText.UnableToUpdateUser
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

        if (membership.Id == context.ActorMembershipId)
        {

            return new ManagementUserDeleteResult
            {
                Forbidden = true,
                CannotDeleteSelf = true,
                BlockReason = ManagementUserActionBlockReason.SelfProtected,
                ErrorMessage = "You cannot remove your own membership."
            };
        }

        var roleCode = membership.ManagementCompanyRole?.Code ?? string.Empty;
        if (IsOwnerRole(roleCode))
        {

            return new ManagementUserDeleteResult
            {
                Forbidden = true,
                CannotDeleteOwner = true,
                BlockReason = ManagementUserActionBlockReason.OwnerProtected,
                ErrorMessage = "The current owner cannot be removed through the standard delete flow. Use ownership transfer instead."
            };
        }

        _dbContext.ManagementCompanyUsers.Remove(membership);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ManagementUserDeleteResult
        {
            Success = true
        };
    }

    /// <inheritdoc />
    public async Task<OwnershipTransferCandidateListResult> GetOwnershipTransferCandidatesAsync(
        ManagementUserAdminAuthorizedContext context,
        CancellationToken cancellationToken = default)
    {
        if (!context.IsOwner)
        {
            return new OwnershipTransferCandidateListResult
            {
                Forbidden = true,
                ErrorMessage = "Only the current owner can transfer ownership."
            };
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var candidates = await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Where(m => m.ManagementCompanyId == context.ManagementCompanyId && m.Id != context.ActorMembershipId)
            .Include(m => m.AppUser)
            .Include(m => m.ManagementCompanyRole)
            .ToListAsync(cancellationToken);

        var items = candidates
            .Where(m => !IsOwnerRole(m.ManagementCompanyRole?.Code ?? string.Empty))
            .Where(m => IsMembershipEffective(m.IsActive, m.ValidFrom, m.ValidTo, today))
            .OrderBy(m => m.AppUser!.LastName)
            .ThenBy(m => m.AppUser!.FirstName)
            .Select(m => new OwnershipTransferCandidate
            {
                MembershipId = m.Id,
                AppUserId = m.AppUserId,
                FullName = $"{m.AppUser!.FirstName} {m.AppUser.LastName}",
                Email = m.AppUser.Email ?? string.Empty,
                RoleId = m.ManagementCompanyRoleId,
                RoleCode = m.ManagementCompanyRole?.Code ?? string.Empty,
                RoleLabel = m.ManagementCompanyRole?.Label.ToString() ?? string.Empty,
                IsEffective = true
            })
            .ToList();

        return new OwnershipTransferCandidateListResult
        {
            Success = true,
            Candidates = items
        };
    }

    /// <inheritdoc />
    public async Task<OwnershipTransferResult> TransferOwnershipAsync(
        ManagementUserAdminAuthorizedContext context,
        TransferOwnershipRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!context.IsOwner)
        {

            return new OwnershipTransferResult
            {
                Forbidden = true,
                ErrorMessage = "Only the current owner can transfer ownership."
            };
        }

        if (request.TargetMembershipId == context.ActorMembershipId)
        {
            return new OwnershipTransferResult
            {
                TargetNotEligibleForOwnership = true,
                ErrorMessage = "Ownership transfer target must be another effective company member."
            };
        }

        var managerRole = await _dbContext.ManagementCompanyRoles
            .FirstOrDefaultAsync(r => r.Code == ManagerRoleCode, cancellationToken);

        if (managerRole == null)
        {
            return new OwnershipTransferResult
            {
                Forbidden = true,
                ErrorMessage = "Management roles are not configured correctly."
            };
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var memberships = await _dbContext.ManagementCompanyUsers
            .AsTracking()
            .Where(m => m.ManagementCompanyId == context.ManagementCompanyId
                        && (m.Id == context.ActorMembershipId || m.Id == request.TargetMembershipId))
            .Include(m => m.ManagementCompanyRole)
            .ToListAsync(cancellationToken);

        var currentOwner = memberships.SingleOrDefault(m => m.Id == context.ActorMembershipId);
        var target = memberships.SingleOrDefault(m => m.Id == request.TargetMembershipId);

        if (currentOwner == null || target == null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new OwnershipTransferResult
            {
                NotFound = true,
                ErrorMessage = "Ownership transfer target was not found in this company."
            };
        }

        if (!IsOwnerRole(currentOwner.ManagementCompanyRole?.Code ?? string.Empty))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new OwnershipTransferResult
            {
                Forbidden = true,
                ErrorMessage = "Only the current active owner can transfer ownership."
            };
        }

        if (IsOwnerRole(target.ManagementCompanyRole?.Code ?? string.Empty)
            || !IsMembershipEffective(target.IsActive, target.ValidFrom, target.ValidTo, today))
        {

            await transaction.RollbackAsync(cancellationToken);
            return new OwnershipTransferResult
            {
                TargetNotEligibleForOwnership = true,
                ErrorMessage = "Ownership transfer target must be an active effective non-owner company member."
            };
        }

        currentOwner.ManagementCompanyRoleId = managerRole.Id;
        currentOwner.ManagementCompanyRole = managerRole;
        target.ManagementCompanyRoleId = context.ActorRoleId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var ownerCount = await CountEffectiveOwnersAsync(context.ManagementCompanyId, cancellationToken);
        if (ownerCount != 1)
        {

            await transaction.RollbackAsync(cancellationToken);
            return new OwnershipTransferResult
            {
                Forbidden = true,
                ErrorMessage = "Ownership transfer failed because the single-owner invariant could not be preserved."
            };
        }

        await transaction.CommitAsync(cancellationToken);

        return new OwnershipTransferResult
        {
            Success = true,
            PreviousOwnerMembershipId = currentOwner.Id,
            NewOwnerMembershipId = target.Id
        };
    }

    private static string NormalizePossiblySerializedLangStr(string value)
    {
        var trimmed = value.Trim();
        if (!trimmed.StartsWith("{", StringComparison.Ordinal))
        {
            return value;
        }

        try
        {
            var nested = JsonSerializer.Deserialize<LangStr>(trimmed, (JsonSerializerOptions?)null);
            if (nested == null)
            {
                return value;
            }

            var localized = nested.ToString();
            return string.IsNullOrWhiteSpace(localized) ? value : localized;
        }
        catch
        {
            return value;
        }
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

    private async Task<(ManagementAreaAuthorizationResult? Result, ManagementMembershipContext? MembershipContext)> ResolveMembershipContextAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = companySlug.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return (new ManagementAreaAuthorizationResult
            {
                CompanyNotFound = true,
                FailureReason = ManagementAuthorizationFailureReason.CompanyNotFound,
                ErrorMessage = "Company slug is required."
            }, null);
        }

        var company = await _dbContext.ManagementCompanies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == normalizedSlug, cancellationToken);

        if (company == null)
        {
            return (new ManagementAreaAuthorizationResult
            {
                CompanyNotFound = true,
                FailureReason = ManagementAuthorizationFailureReason.CompanyNotFound,
                ErrorMessage = "Company not found."
            }, null);
        }

        var actorMembership = await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Include(m => m.ManagementCompanyRole)
            .Where(m => m.AppUserId == appUserId && m.ManagementCompanyId == company.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (actorMembership == null)
        {
            return (new ManagementAreaAuthorizationResult
            {
                IsForbidden = true,
                FailureReason = ManagementAuthorizationFailureReason.MembershipNotFound,
                ErrorMessage = "You do not have access to this company."
            }, null);
        }

        if (!actorMembership.IsActive)
        {
            return (new ManagementAreaAuthorizationResult
            {
                IsForbidden = true,
                MembershipInactive = true,
                FailureReason = ManagementAuthorizationFailureReason.MembershipInactive,
                ErrorMessage = "Your company membership is inactive."
            }, null);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!IsMembershipEffective(actorMembership.IsActive, actorMembership.ValidFrom, actorMembership.ValidTo, today))
        {
            return (new ManagementAreaAuthorizationResult
            {
                IsForbidden = true,
                MembershipNotEffective = true,
                FailureReason = ManagementAuthorizationFailureReason.MembershipNotEffective,
                ErrorMessage = "Your company membership is not currently effective."
            }, null);
        }

        var roleCode = actorMembership.ManagementCompanyRole?.Code ?? string.Empty;
        var membershipContext = new ManagementMembershipContext
        {
            AppUserId = appUserId,
            ManagementCompanyId = company.Id,
            CompanySlug = company.Slug,
            CompanyName = company.Name,
            ActorMembershipId = actorMembership.Id,
            ActorRoleId = actorMembership.ManagementCompanyRoleId,
            ActorRoleCode = roleCode,
            ActorRoleLabel = actorMembership.ManagementCompanyRole?.Label.ToString() ?? string.Empty,
            IsOwner = IsOwnerRole(roleCode),
            IsAdmin = AdminRoleCodes.Contains(roleCode),
            ValidFrom = actorMembership.ValidFrom,
            ValidTo = actorMembership.ValidTo
        };

        return (null, membershipContext);
    }

    private static ManagementUserAdminAuthorizationResult ConvertToAdminAuthorizationResult(ManagementAreaAuthorizationResult result)
    {
        return new ManagementUserAdminAuthorizationResult
        {
            IsAuthorized = false,
            IsForbidden = result.IsForbidden,
            CompanyNotFound = result.CompanyNotFound,
            MembershipInactive = result.MembershipInactive,
            MembershipNotEffective = result.MembershipNotEffective,
            FailureReason = result.FailureReason,
            ErrorMessage = result.ErrorMessage
        };
    }

    private static bool IsOwnerRole(string? roleCode)
    {
        return string.Equals(roleCode, OwnerRoleCode, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMembershipEffective(bool isActive, DateOnly validFrom, DateOnly? validTo, DateOnly today)
    {
        if (!isActive)
        {
            return false;
        }

        if (validFrom > today)
        {
            return false;
        }

        if (validTo.HasValue && validTo.Value < today)
        {
            return false;
        }

        return true;
    }

    private static bool IsValidDateRange(DateOnly validFrom, DateOnly? validTo)
    {
        return !validTo.HasValue || validTo.Value >= validFrom;
    }

    private static bool CanAssignRoleInGenericFlow(ManagementUserAdminAuthorizedContext context, string roleCode)
    {
        if (IsOwnerRole(roleCode))
        {
            return false;
        }

        return context.IsOwner || string.Equals(context.ActorRoleCode, ManagerRoleCode, StringComparison.OrdinalIgnoreCase);
    }

    private static ManagementRoleOption MapRoleOption(ManagementCompanyRole role)
    {
        return new ManagementRoleOption
        {
            RoleId = role.Id,
            RoleCode = role.Code,
            RoleLabel = role.Label.ToString()
        };
    }

    private MemberCapabilities ResolveTargetCapabilities(
        ManagementUserAdminAuthorizedContext context,
        Guid membershipId,
        string roleCode,
        bool isActive,
        DateOnly validFrom,
        DateOnly? validTo,
        DateOnly today)
    {
        var isActor = membershipId == context.ActorMembershipId;
        var isOwner = IsOwnerRole(roleCode);
        var isEffective = IsMembershipEffective(isActive, validFrom, validTo, today);

        if (isOwner)
        {
            if (context.IsOwner && isActor)
            {
                return new MemberCapabilities(
                    CanEdit: false,
                    CanDelete: false,
                    CanTransferOwnership: true,
                    CanChangeRole: false,
                    CanDeactivate: false,
                    ProtectedReason: "Owner role cannot be changed here. Use ownership transfer instead.",
                    ProtectedReasonCode: ManagementUserActionBlockReason.OwnershipTransferRequired);
            }

            return new MemberCapabilities(
                CanEdit: false,
                CanDelete: false,
                CanTransferOwnership: false,
                CanChangeRole: false,
                CanDeactivate: false,
                ProtectedReason: "The current owner is protected and cannot be modified in the standard flows.",
                ProtectedReasonCode: ManagementUserActionBlockReason.OwnerProtected);
        }

        if (isActor)
        {
            return new MemberCapabilities(
                CanEdit: true,
                CanDelete: false,
                CanTransferOwnership: false,
                CanChangeRole: false,
                CanDeactivate: false,
                ProtectedReason: "You cannot change your own role, deactivate yourself, or delete your own membership.",
                ProtectedReasonCode: ManagementUserActionBlockReason.SelfProtected);
        }

        return new MemberCapabilities(
            CanEdit: true,
            CanDelete: true,
            CanTransferOwnership: false,
            CanChangeRole: true,
            CanDeactivate: true,
            ProtectedReason: !isEffective ? "This membership is not currently effective." : null,
            ProtectedReasonCode: !isEffective ? ManagementUserActionBlockReason.MembershipNotEffective : ManagementUserActionBlockReason.None);
    }

    private async Task<int> CountEffectiveOwnersAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var ownerRoleIds = await _dbContext.ManagementCompanyRoles
            .AsNoTracking()
            .Where(r => r.Code == OwnerRoleCode)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Where(m => m.ManagementCompanyId == companyId
                        && ownerRoleIds.Contains(m.ManagementCompanyRoleId)
                        && m.IsActive
                        && m.ValidFrom <= today
                        && (!m.ValidTo.HasValue || m.ValidTo >= today))
            .CountAsync(cancellationToken);
    }

    private sealed record MemberCapabilities(
        bool CanEdit,
        bool CanDelete,
        bool CanTransferOwnership,
        bool CanChangeRole,
        bool CanDeactivate,
        string? ProtectedReason,
        ManagementUserActionBlockReason ProtectedReasonCode);
}
