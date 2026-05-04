using System.Text.Json;
using App.BLL.Contracts.Common;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.ManagementCompanies;
using App.BLL.Contracts.ManagementCompanies.Models;
using App.DAL.Contracts;
using App.DAL.DTO.Lookups;
using App.DAL.DTO.ManagementCompanies;
using Base.Domain;
using FluentResults;

namespace App.BLL.Services.ManagementCompanies;

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

    public async Task<Result<CompanyMembershipContext>> AuthorizeManagementAreaAccessAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var resolution = await ResolveMembershipContextAsync(appUserId, companySlug, cancellationToken);
        if (resolution.IsFailed)
        {
            return Result.Fail(resolution.Errors);
        }

        if (!ManagementAreaRoleCodes.Contains(resolution.Value.ActorRoleCode))
        {
            return Result.Fail<CompanyMembershipContext>(WithAuthorizationReason(
                new ForbiddenError("You do not have access to the management area."),
                CompanyMembershipAuthorizationFailureReason.InsufficientPrivileges));
        }

        return Result.Ok(resolution.Value);
    }

    public async Task<Result<CompanyAdminAuthorizedContext>> AuthorizeAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var resolution = await ResolveMembershipContextAsync(appUserId, companySlug, cancellationToken);
        if (resolution.IsFailed)
        {
            return Result.Fail(resolution.Errors);
        }

        if (!AdminRoleCodes.Contains(resolution.Value.ActorRoleCode))
        {
            var error = WithAuthorizationReason(
                new ForbiddenError("You do not have permission to manage company users."),
                CompanyMembershipAuthorizationFailureReason.InsufficientPrivileges);
            error.Metadata["MembershipValidButNotAdmin"] = true;
            return Result.Fail<CompanyAdminAuthorizedContext>(error);
        }

        return Result.Ok(new CompanyAdminAuthorizedContext
        {
            AppUserId = resolution.Value.AppUserId,
            ManagementCompanyId = resolution.Value.ManagementCompanyId,
            CompanySlug = resolution.Value.CompanySlug,
            CompanyName = resolution.Value.CompanyName,
            ActorMembershipId = resolution.Value.ActorMembershipId,
            ActorRoleId = resolution.Value.ActorRoleId,
            ActorRoleCode = resolution.Value.ActorRoleCode,
            ActorRoleLabel = resolution.Value.ActorRoleLabel,
            IsOwner = resolution.Value.IsOwner,
            IsAdmin = true,
            ValidFrom = resolution.Value.ValidFrom,
            ValidTo = resolution.Value.ValidTo
        });
    }

    public async Task<Result<CompanyMembershipListResult>> ListCompanyMembersAsync(
        CompanyAdminAuthorizedContext context,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var members = await _uow.ManagementCompanies.MembersByCompanyAsync(
            context.ManagementCompanyId,
            cancellationToken);

        return Result.Ok(new CompanyMembershipListResult
        {
            Members = members.Select(member => MapMemberListItem(context, member, today)).ToList()
        });
    }

    public async Task<Result<CompanyMembershipEditModel>> GetMembershipForEditAsync(
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
            return Result.Fail<CompanyMembershipEditModel>(new NotFoundError("Membership not found."));
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
            return Result.Fail<CompanyMembershipEditModel>(WithBlockReason(
                new BusinessRuleError(capabilities.ProtectedReason ?? "This membership cannot be edited."),
                capabilities.ProtectedReasonCode));
        }

        var optionsResult = await GetEditRoleOptionsAsync(context, membershipId, cancellationToken);
        var options = optionsResult.IsSuccess ? optionsResult.Value : Array.Empty<CompanyMembershipRoleOption>();

        return Result.Ok(new CompanyMembershipEditModel
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
        });
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

    public async Task<Result<IReadOnlyList<CompanyMembershipRoleOption>>> GetEditRoleOptionsAsync(
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
            return Result.Fail<IReadOnlyList<CompanyMembershipRoleOption>>(new NotFoundError("Membership not found."));
        }

        if (IsOwnerRole(membership.RoleCode))
        {
            return Result.Fail<IReadOnlyList<CompanyMembershipRoleOption>>(WithBlockReason(
                new BusinessRuleError("Owner role cannot be changed in the standard edit flow. Use ownership transfer instead."),
                CompanyMembershipUserActionBlockReason.OwnershipTransferRequired));
        }

        return Result.Ok(await GetAddRoleOptionsAsync(context, cancellationToken));
    }

    public async Task<Result<Guid>> AddUserByEmailAsync(
        CompanyAdminAuthorizedContext context,
        CompanyMembershipAddRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidDateRange(request.ValidFrom, request.ValidTo))
        {
            return Result.Fail<Guid>(WithBlockReason(
                ValidationError("Membership validity range is invalid.", nameof(request.ValidTo)),
                CompanyMembershipUserActionBlockReason.InvalidDateRange));
        }

        var appUserId = await _uow.ManagementCompanies.FindAppUserIdByEmailAsync(
            request.Email.Trim().ToLowerInvariant(),
            cancellationToken);

        if (appUserId is null)
        {
            return Result.Fail<Guid>(new NotFoundError("User with this email does not exist. They must register first."));
        }

        var existingMembership = await _uow.ManagementCompanies.MembershipExistsAsync(
            appUserId.Value,
            context.ManagementCompanyId,
            cancellationToken);

        if (existingMembership)
        {
            return Result.Fail<Guid>(new ConflictError("This user is already a member of this company."));
        }

        var role = await _uow.ManagementCompanies.FindManagementCompanyRoleByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Fail<Guid>(ValidationError("Selected role is invalid.", nameof(request.RoleId)));
        }

        if (!CanAssignRoleInGenericFlow(context, role.Code))
        {
            return Result.Fail<Guid>(WithBlockReason(
                new BusinessRuleError(IsOwnerRole(role.Code)
                    ? "Owner cannot be assigned through the generic add flow."
                    : "Selected role is not allowed for this action."),
                CompanyMembershipUserActionBlockReason.RoleNotAssignable));
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

        return Result.Ok(membershipId);
    }

    public async Task<Result> UpdateMembershipAsync(
        CompanyAdminAuthorizedContext context,
        Guid membershipId,
        CompanyMembershipUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidDateRange(request.ValidFrom, request.ValidTo))
        {
            return Result.Fail(WithBlockReason(
                ValidationError("Membership validity range is invalid.", nameof(request.ValidTo)),
                CompanyMembershipUserActionBlockReason.InvalidDateRange));
        }

        var membership = await _uow.ManagementCompanies.FindMemberByIdAndCompanyAsync(
            membershipId,
            context.ManagementCompanyId,
            cancellationToken);

        if (membership is null)
        {
            return Result.Fail(new NotFoundError("Membership not found."));
        }

        var role = await _uow.ManagementCompanies.FindManagementCompanyRoleByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Fail(ValidationError("Selected role is invalid.", nameof(request.RoleId)));
        }

        var isSelf = membership.Id == context.ActorMembershipId;
        if (IsOwnerRole(membership.RoleCode))
        {
            return Result.Fail(WithBlockReason(
                new BusinessRuleError("Owner role cannot be changed in the standard edit flow. Use ownership transfer instead."),
                CompanyMembershipUserActionBlockReason.OwnershipTransferRequired));
        }

        if (isSelf && !string.Equals(membership.RoleCode, role.Code, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Fail(WithBlockReason(
                new BusinessRuleError("You cannot change your own role through the generic edit flow."),
                CompanyMembershipUserActionBlockReason.SelfProtected));
        }

        if (isSelf && membership.IsActive && !request.IsActive)
        {
            return Result.Fail(WithBlockReason(
                new BusinessRuleError("You cannot deactivate your own membership."),
                CompanyMembershipUserActionBlockReason.SelfProtected));
        }

        if (IsOwnerRole(role.Code))
        {
            return Result.Fail(WithBlockReason(
                new BusinessRuleError("Owner cannot be assigned through the generic edit flow."),
                CompanyMembershipUserActionBlockReason.RoleNotAssignable));
        }

        if (!CanAssignRoleInGenericFlow(context, role.Code))
        {
            return Result.Fail(WithBlockReason(
                ValidationError("Selected role is not allowed for this action.", nameof(request.RoleId)),
                CompanyMembershipUserActionBlockReason.RoleNotAssignable));
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
            return Result.Fail(new NotFoundError("Membership not found."));
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> DeleteMembershipAsync(
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
            return Result.Fail(new NotFoundError("Membership not found."));
        }

        if (membership.Id == context.ActorMembershipId)
        {
            return Result.Fail(WithBlockReason(
                new BusinessRuleError("You cannot remove your own membership."),
                CompanyMembershipUserActionBlockReason.SelfProtected));
        }

        if (IsOwnerRole(membership.RoleCode))
        {
            return Result.Fail(WithBlockReason(
                new BusinessRuleError("The current owner cannot be removed through the standard delete flow. Use ownership transfer instead."),
                CompanyMembershipUserActionBlockReason.OwnerProtected));
        }

        await _uow.ManagementCompanies.RemoveMembershipAsync(
            membershipId,
            context.ManagementCompanyId,
            cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<IReadOnlyList<OwnershipTransferCandidate>>> GetOwnershipTransferCandidatesAsync(
        CompanyAdminAuthorizedContext context,
        CancellationToken cancellationToken = default)
    {
        if (!context.IsOwner)
        {
            return Result.Fail<IReadOnlyList<OwnershipTransferCandidate>>(new BusinessRuleError("Only the current owner can transfer ownership."));
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var members = await _uow.ManagementCompanies.MembersByCompanyAsync(
            context.ManagementCompanyId,
            cancellationToken);

        return Result.Ok((IReadOnlyList<OwnershipTransferCandidate>)members
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
                .ToList());
    }

    public async Task<Result<OwnershipTransferModel>> TransferOwnershipAsync(
        CompanyAdminAuthorizedContext context,
        TransferOwnershipRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!context.IsOwner)
        {
            return Result.Fail<OwnershipTransferModel>(new BusinessRuleError("Only the current owner can transfer ownership."));
        }

        if (request.TargetMembershipId == context.ActorMembershipId)
        {
            return Result.Fail<OwnershipTransferModel>(WithBlockReason(
                new BusinessRuleError("Ownership transfer target must be another effective company member."),
                CompanyMembershipUserActionBlockReason.TargetNotEligibleForOwnership));
        }

        var managerRole = await _uow.Lookups.FindManagementCompanyRoleByCodeAsync(ManagerRoleCode, cancellationToken);
        if (managerRole is null)
        {
            return Result.Fail<OwnershipTransferModel>(new BusinessRuleError("Management roles are not configured correctly."));
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
            return Result.Fail<OwnershipTransferModel>(new NotFoundError("Ownership transfer target was not found in this company."));
        }

        if (!IsOwnerRole(currentOwner.RoleCode))
        {
            return Result.Fail<OwnershipTransferModel>(new BusinessRuleError("Only the current active owner can transfer ownership."));
        }

        if (IsOwnerRole(target.RoleCode)
            || !IsMembershipEffective(target.IsActive, target.ValidFrom, target.ValidTo, today))
        {
            return Result.Fail<OwnershipTransferModel>(WithBlockReason(
                new BusinessRuleError("Ownership transfer target must be an active effective non-owner company member."),
                CompanyMembershipUserActionBlockReason.TargetNotEligibleForOwnership));
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
                return Result.Fail<OwnershipTransferModel>(new BusinessRuleError("Ownership transfer failed because the single-owner invariant could not be preserved."));
            }

            await _uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return Result.Ok(new OwnershipTransferModel
        {
            PreviousOwnerMembershipId = currentOwner.Id,
            NewOwnerMembershipId = target.Id
        });
    }

    public async Task<IReadOnlyList<CompanyMembershipRoleOption>> GetAvailableRolesAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await _uow.ManagementCompanies.AllManagementCompanyRolesAsync(cancellationToken);
        return roles.Select(MapRoleOption).ToList();
    }

    public async Task<Result<PendingAccessRequestListResult>> GetPendingAccessRequestsAsync(
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

        return Result.Ok(new PendingAccessRequestListResult
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
        });
    }

    public Task<Result> ApprovePendingAccessRequestAsync(
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

    public Task<Result> RejectPendingAccessRequestAsync(
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

    private async Task<Result> ResolvePendingAccessRequestAsync(
        CompanyAdminAuthorizedContext context,
        Guid requestId,
        string targetStatusCode,
        bool createMembership,
        CancellationToken cancellationToken)
    {
        if (!AdminRoleCodes.Contains(context.ActorRoleCode))
        {
            return Result.Fail(new ForbiddenError(L("NoPermissionToResolveAccessRequests", "You do not have permission to resolve access requests.")));
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
            return Result.Fail(new NotFoundError(L("JoinRequestNotFound", "Join request not found.")));
        }

        if (!string.Equals(joinRequest.StatusCode, ManagementCompanyJoinRequestStatusCodes.Pending, StringComparison.OrdinalIgnoreCase)
            && joinRequest.StatusId != pendingStatus.Id)
        {
            return Result.Fail(new ConflictError(L("JoinRequestAlreadyResolved", "Join request is already resolved.")));
        }

        var requesterMembershipExists = await _uow.ManagementCompanies.MembershipExistsAsync(
            joinRequest.AppUserId,
            context.ManagementCompanyId,
            cancellationToken);

        if (requesterMembershipExists)
        {
            return Result.Fail(new ConflictError(L("RequesterAlreadyMemberOfThisCompany", "Requester is already a member of this company.")));
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

        return Result.Ok();
    }

    private async Task<Result<CompanyMembershipContext>> ResolveMembershipContextAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = companySlug.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return Result.Fail<CompanyMembershipContext>(WithAuthorizationReason(
                new NotFoundError("Company slug is required."),
                CompanyMembershipAuthorizationFailureReason.CompanyNotFound));
        }

        var company = await _uow.ManagementCompanies.FirstBySlugAsync(normalizedSlug, cancellationToken);
        if (company is null)
        {
            return Result.Fail<CompanyMembershipContext>(WithAuthorizationReason(
                new NotFoundError("Company not found."),
                CompanyMembershipAuthorizationFailureReason.CompanyNotFound));
        }

        var actorMembership = await _uow.ManagementCompanies.FirstMembershipByUserAndCompanyAsync(
            appUserId,
            company.Id,
            cancellationToken);

        if (actorMembership is null)
        {
            return Result.Fail<CompanyMembershipContext>(WithAuthorizationReason(
                new ForbiddenError("You do not have access to this company."),
                CompanyMembershipAuthorizationFailureReason.MembershipNotFound));
        }

        if (!actorMembership.IsActive)
        {
            return Result.Fail<CompanyMembershipContext>(WithAuthorizationReason(
                new ForbiddenError("Your company membership is inactive."),
                CompanyMembershipAuthorizationFailureReason.MembershipInactive));
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!IsMembershipEffective(actorMembership.IsActive, actorMembership.ValidFrom, actorMembership.ValidTo, today))
        {
            return Result.Fail<CompanyMembershipContext>(WithAuthorizationReason(
                new ForbiddenError("Your company membership is not currently effective."),
                CompanyMembershipAuthorizationFailureReason.MembershipNotEffective));
        }

        return Result.Ok(new CompanyMembershipContext
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

    private static Error WithAuthorizationReason(Error error, CompanyMembershipAuthorizationFailureReason reason)
    {
        error.Metadata["FailureReason"] = reason;
        return error;
    }

    private static Error WithBlockReason(Error error, CompanyMembershipUserActionBlockReason reason)
    {
        error.Metadata["BlockReason"] = reason;
        return error;
    }

    private static ValidationAppError ValidationError(string message, string propertyName)
    {
        return new ValidationAppError(
            message,
            [
                new ValidationFailureModel
                {
                    PropertyName = propertyName,
                    ErrorMessage = message
                }
            ]);
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
