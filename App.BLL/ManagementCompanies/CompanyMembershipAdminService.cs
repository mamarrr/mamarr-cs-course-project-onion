using System.Text.Json;
using App.BLL.Contracts.ManagementCompanies;
using App.BLL.Contracts.ManagementCompanies.Models;
using App.BLL.Contracts.ManagementCompanies.Services;
using App.Contracts;
using App.Contracts.DAL.Lookups;
using App.Contracts.DAL.ManagementCompanies;
using Base.Domain;

namespace App.BLL.ManagementCompanies;

public class CompanyMembershipAdminService :
    ICompanyMembershipAdminService,
    IManagementCompanyAccessService,
    ICompanyMembershipAuthorizationService,
    ICompanyMembershipQueryService,
    ICompanyMembershipCommandService,
    ICompanyRoleOptionsService,
    ICompanyOwnershipTransferService,
    ICompanyAccessRequestReviewService
{
    private const string OwnerRoleCode = "OWNER";
    private const string ManagerRoleCode = "MANAGER";

    private readonly IAppUOW _uow;

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

    public CompanyMembershipAdminService(IAppUOW uow)
    {
        _uow = uow;
    }

    public async Task<CompanyAreaAuthorizationResult> AuthorizeManagementAreaAccessAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var resolution = await ResolveMembershipContextAsync(appUserId, companySlug, cancellationToken);
        if (resolution.Result is not null)
        {
            return resolution.Result;
        }

        if (!ManagementAreaRoleCodes.Contains(resolution.MembershipContext!.ActorRoleCode))
        {
            return new CompanyAreaAuthorizationResult
            {
                IsForbidden = true,
                FailureReason = CompanyMembershipAuthorizationFailureReason.InsufficientPrivileges,
                ErrorMessage = "You do not have access to the management area."
            };
        }

        return new CompanyAreaAuthorizationResult
        {
            IsAuthorized = true,
            Context = resolution.MembershipContext
        };
    }

    public async Task<CompanyAdminAuthorizationResult> AuthorizeAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var resolution = await ResolveMembershipContextAsync(appUserId, companySlug, cancellationToken);
        if (resolution.Result is not null)
        {
            return ConvertToAdminAuthorizationResult(resolution.Result);
        }

        if (!AdminRoleCodes.Contains(resolution.MembershipContext!.ActorRoleCode))
        {
            return new CompanyAdminAuthorizationResult
            {
                IsForbidden = true,
                MembershipValidButNotAdmin = true,
                FailureReason = CompanyMembershipAuthorizationFailureReason.InsufficientPrivileges,
                ErrorMessage = "You do not have permission to manage company users."
            };
        }

        return new CompanyAdminAuthorizationResult
        {
            IsAuthorized = true,
            Context = new CompanyAdminAuthorizedContext
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

    public async Task<CompanyMembershipListResult> ListCompanyMembersAsync(
        CompanyAdminAuthorizedContext context,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var members = await _uow.ManagementCompanies.MembersByCompanyAsync(
            context.ManagementCompanyId,
            cancellationToken);

        return new CompanyMembershipListResult
        {
            Members = members.Select(member => MapMemberListItem(context, member, today)).ToList()
        };
    }

    public async Task<CompanyMembershipEditResult> GetMembershipForEditAsync(
        CompanyAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var membership = await _uow.ManagementCompanies.FindMemberByIdAndCompanyAsync(
            membershipId,
            context.ManagementCompanyId,
            cancellationToken);

        if (membership is null)
        {
            return new CompanyMembershipEditResult
            {
                NotFound = true,
                ErrorMessage = "Membership not found."
            };
        }

        var capabilities = ResolveTargetCapabilities(
            context,
            membership.Id,
            membership.RoleCode,
            membership.IsActive,
            membership.ValidFrom,
            membership.ValidTo,
            today);

        if (!capabilities.CanEdit && !capabilities.CanTransferOwnership)
        {
            return new CompanyMembershipEditResult
            {
                Forbidden = true,
                ErrorMessage = capabilities.ProtectedReason ?? "This membership cannot be edited."
            };
        }

        var optionsResult = await GetEditRoleOptionsAsync(context, membershipId, cancellationToken);
        var options = optionsResult.Success ? optionsResult.Options : Array.Empty<CompanyMembershipRoleOption>();

        return new CompanyMembershipEditResult
        {
            Success = true,
            Data = new CompanyMembershipEditModel
            {
                MembershipId = membership.Id,
                AppUserId = membership.AppUserId,
                FullName = FullName(membership),
                Email = membership.Email,
                RoleId = membership.RoleId,
                RoleCode = membership.RoleCode,
                RoleLabel = membership.RoleLabel,
                JobTitle = NormalizePossiblySerializedLangStr(membership.JobTitle) ?? string.Empty,
                IsActive = membership.IsActive,
                ValidFrom = membership.ValidFrom,
                ValidTo = membership.ValidTo,
                IsOwner = IsOwnerRole(membership.RoleCode),
                IsActor = membership.Id == context.ActorMembershipId,
                IsEffective = IsMembershipEffective(membership.IsActive, membership.ValidFrom, membership.ValidTo, today),
                CanEdit = capabilities.CanEdit,
                CanDelete = capabilities.CanDelete,
                CanTransferOwnership = capabilities.CanTransferOwnership,
                CanChangeRole = capabilities.CanChangeRole,
                CanDeactivate = capabilities.CanDeactivate,
                OwnershipTransferRequired = capabilities.ProtectedReasonCode == CompanyMembershipUserActionBlockReason.OwnershipTransferRequired,
                ProtectedReason = capabilities.ProtectedReason,
                ProtectedReasonCode = capabilities.ProtectedReasonCode,
                AvailableRoleOptions = options
            }
        };
    }

    public async Task<IReadOnlyList<CompanyMembershipRoleOption>> GetAddRoleOptionsAsync(
        CompanyAdminAuthorizedContext context,
        CancellationToken cancellationToken = default)
    {
        var roles = await _uow.ManagementCompanies.AllManagementCompanyRolesAsync(cancellationToken);
        return roles
            .Where(role => CanAssignRoleInGenericFlow(context, role.Code))
            .Select(MapRoleOption)
            .ToList();
    }

    public async Task<CompanyMembershipOptionsResult> GetEditRoleOptionsAsync(
        CompanyAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default)
    {
        var membership = await _uow.ManagementCompanies.FindMemberByIdAndCompanyAsync(
            membershipId,
            context.ManagementCompanyId,
            cancellationToken);

        if (membership is null)
        {
            return new CompanyMembershipOptionsResult
            {
                NotFound = true,
                ErrorMessage = "Membership not found."
            };
        }

        if (IsOwnerRole(membership.RoleCode))
        {
            return new CompanyMembershipOptionsResult
            {
                Forbidden = true,
                OwnershipTransferRequired = true,
                ErrorMessage = "Owner role cannot be changed in the standard edit flow. Use ownership transfer instead."
            };
        }

        return new CompanyMembershipOptionsResult
        {
            Success = true,
            Options = await GetAddRoleOptionsAsync(context, cancellationToken)
        };
    }

    public async Task<CompanyMembershipAddResult> AddUserByEmailAsync(
        CompanyAdminAuthorizedContext context,
        CompanyMembershipAddRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidDateRange(request.ValidFrom, request.ValidTo))
        {
            return new CompanyMembershipAddResult
            {
                InvalidDateRange = true,
                ErrorMessage = "Membership validity range is invalid."
            };
        }

        var appUserId = await _uow.ManagementCompanies.FindAppUserIdByEmailAsync(
            request.Email.Trim().ToLowerInvariant(),
            cancellationToken);

        if (appUserId is null)
        {
            return new CompanyMembershipAddResult
            {
                UserNotFound = true,
                ErrorMessage = "User with this email does not exist. They must register first."
            };
        }

        var existingMembership = await _uow.ManagementCompanies.MembershipExistsAsync(
            appUserId.Value,
            context.ManagementCompanyId,
            cancellationToken);

        if (existingMembership)
        {
            return new CompanyMembershipAddResult
            {
                DuplicateMembership = true,
                ErrorMessage = "This user is already a member of this company."
            };
        }

        var role = await _uow.ManagementCompanies.FindManagementCompanyRoleByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return new CompanyMembershipAddResult
            {
                InvalidRole = true,
                ErrorMessage = "Selected role is invalid."
            };
        }

        if (!CanAssignRoleInGenericFlow(context, role.Code))
        {
            return new CompanyMembershipAddResult
            {
                InvalidRole = true,
                CannotAssignOwner = IsOwnerRole(role.Code),
                ErrorMessage = IsOwnerRole(role.Code)
                    ? "Owner cannot be assigned through the generic add flow."
                    : "Selected role is not allowed for this action."
            };
        }

        var membershipId = Guid.NewGuid();
        _uow.ManagementCompanies.AddMembership(new ManagementCompanyMembershipCreateDalDto
        {
            Id = membershipId,
            ManagementCompanyId = context.ManagementCompanyId,
            AppUserId = appUserId.Value,
            RoleId = request.RoleId,
            JobTitle = request.JobTitle.Trim(),
            IsActive = request.IsActive,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            CreatedAt = DateTime.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);

        return new CompanyMembershipAddResult
        {
            Success = true,
            CreatedMembershipId = membershipId
        };
    }

    public async Task<CompanyMembershipUpdateResult> UpdateMembershipAsync(
        CompanyAdminAuthorizedContext context,
        Guid membershipId,
        CompanyMembershipUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidDateRange(request.ValidFrom, request.ValidTo))
        {
            return new CompanyMembershipUpdateResult
            {
                InvalidDateRange = true,
                BlockReason = CompanyMembershipUserActionBlockReason.InvalidDateRange,
                ErrorMessage = "Membership validity range is invalid."
            };
        }

        var membership = await _uow.ManagementCompanies.FindMemberByIdAndCompanyAsync(
            membershipId,
            context.ManagementCompanyId,
            cancellationToken);

        if (membership is null)
        {
            return new CompanyMembershipUpdateResult
            {
                NotFound = true,
                ErrorMessage = "Membership not found."
            };
        }

        var role = await _uow.ManagementCompanies.FindManagementCompanyRoleByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return new CompanyMembershipUpdateResult
            {
                InvalidRole = true,
                ErrorMessage = "Selected role is invalid."
            };
        }

        var isSelf = membership.Id == context.ActorMembershipId;
        if (IsOwnerRole(membership.RoleCode))
        {
            return new CompanyMembershipUpdateResult
            {
                Forbidden = true,
                CannotEditOwner = true,
                OwnershipTransferRequired = true,
                BlockReason = CompanyMembershipUserActionBlockReason.OwnershipTransferRequired,
                ErrorMessage = "Owner role cannot be changed in the standard edit flow. Use ownership transfer instead."
            };
        }

        if (isSelf && !string.Equals(membership.RoleCode, role.Code, StringComparison.OrdinalIgnoreCase))
        {
            return new CompanyMembershipUpdateResult
            {
                Forbidden = true,
                CannotChangeOwnRole = true,
                BlockReason = CompanyMembershipUserActionBlockReason.SelfProtected,
                ErrorMessage = "You cannot change your own role through the generic edit flow."
            };
        }

        if (isSelf && membership.IsActive && !request.IsActive)
        {
            return new CompanyMembershipUpdateResult
            {
                Forbidden = true,
                CannotDeactivateSelf = true,
                BlockReason = CompanyMembershipUserActionBlockReason.SelfProtected,
                ErrorMessage = "You cannot deactivate your own membership."
            };
        }

        if (IsOwnerRole(role.Code))
        {
            return new CompanyMembershipUpdateResult
            {
                Forbidden = true,
                CannotAssignOwner = true,
                BlockReason = CompanyMembershipUserActionBlockReason.RoleNotAssignable,
                ErrorMessage = "Owner cannot be assigned through the generic edit flow."
            };
        }

        if (!CanAssignRoleInGenericFlow(context, role.Code))
        {
            return new CompanyMembershipUpdateResult
            {
                Forbidden = true,
                InvalidRole = true,
                BlockReason = CompanyMembershipUserActionBlockReason.RoleNotAssignable,
                ErrorMessage = "Selected role is not allowed for this action."
            };
        }

        var updated = await _uow.ManagementCompanies.ApplyMembershipUpdateAsync(
            new ManagementCompanyMembershipUpdateDalDto
            {
                MembershipId = membershipId,
                ManagementCompanyId = context.ManagementCompanyId,
                RoleId = request.RoleId,
                JobTitle = request.JobTitle,
                IsActive = request.IsActive,
                ValidFrom = request.ValidFrom,
                ValidTo = request.ValidTo
            },
            cancellationToken);

        if (!updated)
        {
            return new CompanyMembershipUpdateResult
            {
                NotFound = true,
                ErrorMessage = "Membership not found."
            };
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return new CompanyMembershipUpdateResult { Success = true };
    }

    public async Task<CompanyMembershipDeleteResult> DeleteMembershipAsync(
        CompanyAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default)
    {
        var membership = await _uow.ManagementCompanies.FindMemberByIdAndCompanyAsync(
            membershipId,
            context.ManagementCompanyId,
            cancellationToken);

        if (membership is null)
        {
            return new CompanyMembershipDeleteResult
            {
                NotFound = true,
                ErrorMessage = "Membership not found."
            };
        }

        if (membership.Id == context.ActorMembershipId)
        {
            return new CompanyMembershipDeleteResult
            {
                Forbidden = true,
                CannotDeleteSelf = true,
                BlockReason = CompanyMembershipUserActionBlockReason.SelfProtected,
                ErrorMessage = "You cannot remove your own membership."
            };
        }

        if (IsOwnerRole(membership.RoleCode))
        {
            return new CompanyMembershipDeleteResult
            {
                Forbidden = true,
                CannotDeleteOwner = true,
                BlockReason = CompanyMembershipUserActionBlockReason.OwnerProtected,
                ErrorMessage = "The current owner cannot be removed through the standard delete flow. Use ownership transfer instead."
            };
        }

        await _uow.ManagementCompanies.RemoveMembershipAsync(
            membershipId,
            context.ManagementCompanyId,
            cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new CompanyMembershipDeleteResult { Success = true };
    }

    public async Task<OwnershipTransferCandidateListResult> GetOwnershipTransferCandidatesAsync(
        CompanyAdminAuthorizedContext context,
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
        var members = await _uow.ManagementCompanies.MembersByCompanyAsync(
            context.ManagementCompanyId,
            cancellationToken);

        return new OwnershipTransferCandidateListResult
        {
            Success = true,
            Candidates = members
                .Where(member => member.Id != context.ActorMembershipId)
                .Where(member => !IsOwnerRole(member.RoleCode))
                .Where(member => IsMembershipEffective(member.IsActive, member.ValidFrom, member.ValidTo, today))
                .OrderBy(member => member.LastName)
                .ThenBy(member => member.FirstName)
                .Select(member => new OwnershipTransferCandidate
                {
                    MembershipId = member.Id,
                    AppUserId = member.AppUserId,
                    FullName = FullName(member),
                    Email = member.Email,
                    RoleId = member.RoleId,
                    RoleCode = member.RoleCode,
                    RoleLabel = member.RoleLabel,
                    IsEffective = true
                })
                .ToList()
        };
    }

    public async Task<OwnershipTransferResult> TransferOwnershipAsync(
        CompanyAdminAuthorizedContext context,
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

        var managerRole = await _uow.Lookups.FindManagementCompanyRoleByCodeAsync(ManagerRoleCode, cancellationToken);
        if (managerRole is null)
        {
            return new OwnershipTransferResult
            {
                Forbidden = true,
                ErrorMessage = "Management roles are not configured correctly."
            };
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var memberships = await _uow.ManagementCompanies.FindMembersByIdsAndCompanyAsync(
            context.ManagementCompanyId,
            [context.ActorMembershipId, request.TargetMembershipId],
            cancellationToken);

        var currentOwner = memberships.SingleOrDefault(member => member.Id == context.ActorMembershipId);
        var target = memberships.SingleOrDefault(member => member.Id == request.TargetMembershipId);

        if (currentOwner is null || target is null)
        {
            return new OwnershipTransferResult
            {
                NotFound = true,
                ErrorMessage = "Ownership transfer target was not found in this company."
            };
        }

        if (!IsOwnerRole(currentOwner.RoleCode))
        {
            return new OwnershipTransferResult
            {
                Forbidden = true,
                ErrorMessage = "Only the current active owner can transfer ownership."
            };
        }

        if (IsOwnerRole(target.RoleCode)
            || !IsMembershipEffective(target.IsActive, target.ValidFrom, target.ValidTo, today))
        {
            return new OwnershipTransferResult
            {
                TargetNotEligibleForOwnership = true,
                ErrorMessage = "Ownership transfer target must be an active effective non-owner company member."
            };
        }

        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            await _uow.ManagementCompanies.SetMembershipRoleAsync(
                currentOwner.Id,
                context.ManagementCompanyId,
                managerRole.Id,
                cancellationToken);
            await _uow.ManagementCompanies.SetMembershipRoleAsync(
                target.Id,
                context.ManagementCompanyId,
                context.ActorRoleId,
                cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            var ownerCount = await _uow.ManagementCompanies.CountEffectiveOwnersAsync(
                context.ManagementCompanyId,
                cancellationToken);
            if (ownerCount != 1)
            {
                await _uow.RollbackTransactionAsync(cancellationToken);
                return new OwnershipTransferResult
                {
                    Forbidden = true,
                    ErrorMessage = "Ownership transfer failed because the single-owner invariant could not be preserved."
                };
            }

            await _uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return new OwnershipTransferResult
        {
            Success = true,
            PreviousOwnerMembershipId = currentOwner.Id,
            NewOwnerMembershipId = target.Id
        };
    }

    public async Task<IReadOnlyList<CompanyMembershipRoleOption>> GetAvailableRolesAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await _uow.ManagementCompanies.AllManagementCompanyRolesAsync(cancellationToken);
        return roles.Select(MapRoleOption).ToList();
    }

    public async Task<PendingAccessRequestListResult> GetPendingAccessRequestsAsync(
        CompanyAdminAuthorizedContext context,
        CancellationToken cancellationToken = default)
    {
        var pendingStatus = await GetJoinRequestStatusAsync(
            ManagementCompanyJoinRequestStatusCodes.Pending,
            cancellationToken);
        var requests = await _uow.ManagementCompanyJoinRequests.PendingByCompanyAsync(
            context.ManagementCompanyId,
            pendingStatus.Id,
            cancellationToken);

        return new PendingAccessRequestListResult
        {
            Requests = requests.Select(request => new PendingAccessRequestItem
            {
                RequestId = request.Id,
                AppUserId = request.AppUserId,
                RequesterName = $"{request.RequesterFirstName} {request.RequesterLastName}".Trim(),
                RequesterEmail = request.RequesterEmail,
                RequestedRoleCode = request.RequestedRoleCode,
                RequestedRoleLabel = request.RequestedRoleLabel,
                Message = NormalizePossiblySerializedLangStr(request.Message),
                RequestedAt = request.CreatedAt
            }).ToList()
        };
    }

    public Task<PendingAccessRequestActionResult> ApprovePendingAccessRequestAsync(
        CompanyAdminAuthorizedContext context,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        return ResolvePendingAccessRequestAsync(
            context,
            requestId,
            ManagementCompanyJoinRequestStatusCodes.Approved,
            createMembership: true,
            cancellationToken);
    }

    public Task<PendingAccessRequestActionResult> RejectPendingAccessRequestAsync(
        CompanyAdminAuthorizedContext context,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        return ResolvePendingAccessRequestAsync(
            context,
            requestId,
            ManagementCompanyJoinRequestStatusCodes.Rejected,
            createMembership: false,
            cancellationToken);
    }

    private async Task<PendingAccessRequestActionResult> ResolvePendingAccessRequestAsync(
        CompanyAdminAuthorizedContext context,
        Guid requestId,
        string targetStatusCode,
        bool createMembership,
        CancellationToken cancellationToken)
    {
        if (!AdminRoleCodes.Contains(context.ActorRoleCode))
        {
            return new PendingAccessRequestActionResult
            {
                Forbidden = true,
                ErrorMessage = L("NoPermissionToResolveAccessRequests", "You do not have permission to resolve access requests.")
            };
        }

        var pendingStatus = await GetJoinRequestStatusAsync(
            ManagementCompanyJoinRequestStatusCodes.Pending,
            cancellationToken);
        var targetStatus = await GetJoinRequestStatusAsync(targetStatusCode, cancellationToken);

        var joinRequest = await _uow.ManagementCompanyJoinRequests.FindByIdAndCompanyAsync(
            requestId,
            context.ManagementCompanyId,
            cancellationToken);

        if (joinRequest is null)
        {
            return new PendingAccessRequestActionResult
            {
                NotFound = true,
                ErrorMessage = L("JoinRequestNotFound", "Join request not found.")
            };
        }

        if (!string.Equals(joinRequest.StatusCode, ManagementCompanyJoinRequestStatusCodes.Pending, StringComparison.OrdinalIgnoreCase)
            && joinRequest.StatusId != pendingStatus.Id)
        {
            return new PendingAccessRequestActionResult
            {
                AlreadyResolved = true,
                ErrorMessage = L("JoinRequestAlreadyResolved", "Join request is already resolved.")
            };
        }

        var requesterMembershipExists = await _uow.ManagementCompanies.MembershipExistsAsync(
            joinRequest.AppUserId,
            context.ManagementCompanyId,
            cancellationToken);

        if (requesterMembershipExists)
        {
            return new PendingAccessRequestActionResult
            {
                AlreadyMember = true,
                ErrorMessage = L("RequesterAlreadyMemberOfThisCompany", "Requester is already a member of this company.")
            };
        }

        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            if (createMembership)
            {
                _uow.ManagementCompanies.AddMembership(new ManagementCompanyMembershipCreateDalDto
                {
                    Id = Guid.NewGuid(),
                    ManagementCompanyId = context.ManagementCompanyId,
                    AppUserId = joinRequest.AppUserId,
                    RoleId = joinRequest.RequestedRoleId,
                    JobTitle = "Employee",
                    IsActive = true,
                    ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _uow.ManagementCompanyJoinRequests.SetStatusAsync(
                joinRequest.Id,
                context.ManagementCompanyId,
                targetStatus.Id,
                context.AppUserId,
                DateTime.UtcNow,
                cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
            await _uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return new PendingAccessRequestActionResult { Success = true };
    }

    private async Task<(CompanyAreaAuthorizationResult? Result, CompanyMembershipContext? MembershipContext)> ResolveMembershipContextAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = companySlug.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return (new CompanyAreaAuthorizationResult
            {
                CompanyNotFound = true,
                FailureReason = CompanyMembershipAuthorizationFailureReason.CompanyNotFound,
                ErrorMessage = "Company slug is required."
            }, null);
        }

        var company = await _uow.ManagementCompanies.FirstBySlugAsync(normalizedSlug, cancellationToken);
        if (company is null)
        {
            return (new CompanyAreaAuthorizationResult
            {
                CompanyNotFound = true,
                FailureReason = CompanyMembershipAuthorizationFailureReason.CompanyNotFound,
                ErrorMessage = "Company not found."
            }, null);
        }

        var actorMembership = await _uow.ManagementCompanies.FirstMembershipByUserAndCompanyAsync(
            appUserId,
            company.Id,
            cancellationToken);

        if (actorMembership is null)
        {
            return (new CompanyAreaAuthorizationResult
            {
                IsForbidden = true,
                FailureReason = CompanyMembershipAuthorizationFailureReason.MembershipNotFound,
                ErrorMessage = "You do not have access to this company."
            }, null);
        }

        if (!actorMembership.IsActive)
        {
            return (new CompanyAreaAuthorizationResult
            {
                IsForbidden = true,
                MembershipInactive = true,
                FailureReason = CompanyMembershipAuthorizationFailureReason.MembershipInactive,
                ErrorMessage = "Your company membership is inactive."
            }, null);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!IsMembershipEffective(actorMembership.IsActive, actorMembership.ValidFrom, actorMembership.ValidTo, today))
        {
            return (new CompanyAreaAuthorizationResult
            {
                IsForbidden = true,
                MembershipNotEffective = true,
                FailureReason = CompanyMembershipAuthorizationFailureReason.MembershipNotEffective,
                ErrorMessage = "Your company membership is not currently effective."
            }, null);
        }

        return (null, new CompanyMembershipContext
        {
            AppUserId = appUserId,
            ManagementCompanyId = company.Id,
            CompanySlug = company.Slug,
            CompanyName = company.Name,
            ActorMembershipId = actorMembership.Id,
            ActorRoleId = actorMembership.RoleId,
            ActorRoleCode = actorMembership.RoleCode,
            ActorRoleLabel = actorMembership.RoleLabel,
            IsOwner = IsOwnerRole(actorMembership.RoleCode),
            IsAdmin = AdminRoleCodes.Contains(actorMembership.RoleCode),
            ValidFrom = actorMembership.ValidFrom,
            ValidTo = actorMembership.ValidTo
        });
    }

    private async Task<LookupDalDto> GetJoinRequestStatusAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var status = await _uow.Lookups.FindManagementCompanyJoinRequestStatusByCodeAsync(code, cancellationToken);
        return status ?? throw new InvalidOperationException($"Management company join request status '{code}' is not seeded.");
    }

    private static CompanyMembershipUserListItem MapMemberListItem(
        CompanyAdminAuthorizedContext context,
        ManagementCompanyMembershipDalDto member,
        DateOnly today)
    {
        var capabilities = ResolveTargetCapabilities(
            context,
            member.Id,
            member.RoleCode,
            member.IsActive,
            member.ValidFrom,
            member.ValidTo,
            today);

        return new CompanyMembershipUserListItem
        {
            MembershipId = member.Id,
            AppUserId = member.AppUserId,
            FullName = FullName(member),
            Email = member.Email,
            RoleId = member.RoleId,
            RoleCode = member.RoleCode,
            RoleLabel = member.RoleLabel,
            JobTitle = NormalizePossiblySerializedLangStr(member.JobTitle) ?? string.Empty,
            IsActive = member.IsActive,
            ValidFrom = member.ValidFrom,
            ValidTo = member.ValidTo,
            IsActor = member.Id == context.ActorMembershipId,
            IsOwner = IsOwnerRole(member.RoleCode),
            IsEffective = IsMembershipEffective(member.IsActive, member.ValidFrom, member.ValidTo, today),
            CanEdit = capabilities.CanEdit,
            CanDelete = capabilities.CanDelete,
            CanTransferOwnership = capabilities.CanTransferOwnership,
            CanChangeRole = capabilities.CanChangeRole,
            CanDeactivate = capabilities.CanDeactivate,
            ProtectedReason = capabilities.ProtectedReason,
            ProtectedReasonCode = capabilities.ProtectedReasonCode
        };
    }

    private static CompanyAdminAuthorizationResult ConvertToAdminAuthorizationResult(CompanyAreaAuthorizationResult result)
    {
        return new CompanyAdminAuthorizationResult
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
        return isActive
               && validFrom <= today
               && (!validTo.HasValue || validTo.Value >= today);
    }

    private static bool IsValidDateRange(DateOnly validFrom, DateOnly? validTo)
    {
        return !validTo.HasValue || validTo.Value >= validFrom;
    }

    private static bool CanAssignRoleInGenericFlow(CompanyAdminAuthorizedContext context, string roleCode)
    {
        if (IsOwnerRole(roleCode))
        {
            return false;
        }

        return context.IsOwner || string.Equals(context.ActorRoleCode, ManagerRoleCode, StringComparison.OrdinalIgnoreCase);
    }

    private static CompanyMembershipRoleOption MapRoleOption(LookupDalDto role)
    {
        return new CompanyMembershipRoleOption
        {
            RoleId = role.Id,
            RoleCode = role.Code,
            RoleLabel = role.Label
        };
    }

    private static string FullName(ManagementCompanyMembershipDalDto member)
    {
        return $"{member.FirstName} {member.LastName}".Trim();
    }

    private static string? NormalizePossiblySerializedLangStr(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var trimmed = value.Trim();
        if (!trimmed.StartsWith("{", StringComparison.Ordinal))
        {
            return value;
        }

        try
        {
            var nested = JsonSerializer.Deserialize<LangStr>(trimmed, (JsonSerializerOptions?)null);
            if (nested is null)
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

    private static MemberCapabilities ResolveTargetCapabilities(
        CompanyAdminAuthorizedContext context,
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
                    ProtectedReasonCode: CompanyMembershipUserActionBlockReason.OwnershipTransferRequired);
            }

            return new MemberCapabilities(
                CanEdit: false,
                CanDelete: false,
                CanTransferOwnership: false,
                CanChangeRole: false,
                CanDeactivate: false,
                ProtectedReason: "The current owner is protected and cannot be modified in the standard flows.",
                ProtectedReasonCode: CompanyMembershipUserActionBlockReason.OwnerProtected);
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
                ProtectedReasonCode: CompanyMembershipUserActionBlockReason.SelfProtected);
        }

        return new MemberCapabilities(
            CanEdit: true,
            CanDelete: true,
            CanTransferOwnership: false,
            CanChangeRole: true,
            CanDeactivate: true,
            ProtectedReason: !isEffective ? "This membership is not currently effective." : null,
            ProtectedReasonCode: !isEffective ? CompanyMembershipUserActionBlockReason.MembershipNotEffective : CompanyMembershipUserActionBlockReason.None);
    }

    private static string L(string resourceKey, string fallback)
    {
        return App.Resources.Views.UiText.ResourceManager.GetString(resourceKey, System.Globalization.CultureInfo.CurrentUICulture)
               ?? fallback;
    }

    private sealed record MemberCapabilities(
        bool CanEdit,
        bool CanDelete,
        bool CanTransferOwnership,
        bool CanChangeRole,
        bool CanDeactivate,
        string? ProtectedReason,
        CompanyMembershipUserActionBlockReason ProtectedReasonCode);
}
