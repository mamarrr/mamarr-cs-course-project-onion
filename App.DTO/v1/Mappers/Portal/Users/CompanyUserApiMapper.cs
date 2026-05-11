using App.BLL.DTO.ManagementCompanies.Models;
using App.DTO.v1.Portal.Users;
using Base.Contracts;

namespace App.DTO.v1.Mappers.Portal.Users;

public sealed class CompanyUserApiMapper :
    IBaseMapper<AddCompanyUserDto, CompanyMembershipAddRequest>,
    IBaseMapper<UpdateCompanyUserDto, CompanyMembershipUpdateRequest>
{
    public CompanyUserListItemDto? Map(CompanyMembershipUserListItem? entity)
    {
        return entity is null
            ? null
            : new CompanyUserListItemDto
            {
                MembershipId = entity.MembershipId,
                AppUserId = entity.AppUserId,
                FullName = entity.FullName,
                Email = entity.Email,
                RoleId = entity.RoleId,
                RoleCode = entity.RoleCode,
                RoleLabel = entity.RoleLabel,
                JobTitle = entity.JobTitle,
                ValidFrom = entity.ValidFrom,
                ValidTo = entity.ValidTo,
                IsActor = entity.IsActor,
                IsOwner = entity.IsOwner,
                IsEffective = entity.IsEffective,
                CanEdit = entity.CanEdit,
                CanDelete = entity.CanDelete,
                CanTransferOwnership = entity.CanTransferOwnership,
                CanChangeRole = entity.CanChangeRole,
                CanDeactivate = entity.CanDeactivate,
                ProtectedReason = entity.ProtectedReason,
                ProtectedReasonCode = entity.ProtectedReasonCode.ToString()
            };
    }

    public CompanyUserEditDto? Map(CompanyMembershipEditModel? entity, CompanyAdminAuthorizedContext context)
    {
        return entity is null
            ? null
            : new CompanyUserEditDto
            {
                MembershipId = entity.MembershipId,
                AppUserId = entity.AppUserId,
                CompanySlug = context.CompanySlug,
                CompanyName = context.CompanyName,
                FullName = entity.FullName,
                Email = entity.Email,
                RoleId = entity.RoleId,
                RoleCode = entity.RoleCode,
                RoleLabel = entity.RoleLabel,
                JobTitle = entity.JobTitle,
                ValidFrom = entity.ValidFrom,
                ValidTo = entity.ValidTo,
                IsOwner = entity.IsOwner,
                IsActor = entity.IsActor,
                IsEffective = entity.IsEffective,
                CanEdit = entity.CanEdit,
                CanDelete = entity.CanDelete,
                CanTransferOwnership = entity.CanTransferOwnership,
                CanChangeRole = entity.CanChangeRole,
                CanDeactivate = entity.CanDeactivate,
                OwnershipTransferRequired = entity.OwnershipTransferRequired,
                ProtectedReason = entity.ProtectedReason,
                ProtectedReasonCode = entity.ProtectedReasonCode.ToString(),
                AvailableRoles = entity.AvailableRoleOptions
                    .Select(Map)
                    .OfType<CompanyUserRoleOptionDto>()
                    .ToList()
            };
    }

    public CompanyUserRoleOptionDto? Map(CompanyMembershipRoleOption? entity)
    {
        return entity is null
            ? null
            : new CompanyUserRoleOptionDto
            {
                RoleId = entity.RoleId,
                RoleCode = entity.RoleCode,
                RoleLabel = entity.RoleLabel
            };
    }

    public CompanyUsersPageDto Map(
        CompanyAdminAuthorizedContext context,
        CompanyMembershipListResult members,
        PendingAccessRequestListResult pendingRequests,
        IReadOnlyList<CompanyMembershipRoleOption> roles,
        PendingAccessRequestApiMapper pendingMapper)
    {
        return new CompanyUsersPageDto
        {
            ManagementCompanyId = context.ManagementCompanyId,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            ActorMembershipId = context.ActorMembershipId,
            ActorRoleId = context.ActorRoleId,
            ActorRoleCode = context.ActorRoleCode,
            ActorRoleLabel = context.ActorRoleLabel,
            CurrentActorIsOwner = context.IsOwner,
            CurrentActorIsAdmin = context.IsAdmin,
            Members = members.Members
                .Select(Map)
                .OfType<CompanyUserListItemDto>()
                .ToList(),
            PendingRequests = pendingRequests.Requests
                .Select(pendingMapper.Map)
                .OfType<PendingAccessRequestDto>()
                .ToList(),
            Roles = roles
                .Select(Map)
                .OfType<CompanyUserRoleOptionDto>()
                .ToList()
        };
    }

    public CompanyMembershipAddRequest? Map(AddCompanyUserDto? entity)
    {
        return entity is null
            ? null
            : new CompanyMembershipAddRequest
            {
                Email = entity.Email,
                RoleId = entity.RoleId,
                JobTitle = entity.JobTitle,
                ValidFrom = entity.ValidFrom,
                ValidTo = entity.ValidTo
            };
    }

    public CompanyMembershipUpdateRequest? Map(UpdateCompanyUserDto? entity)
    {
        return entity is null
            ? null
            : new CompanyMembershipUpdateRequest
            {
                RoleId = entity.RoleId,
                JobTitle = entity.JobTitle,
                ValidFrom = entity.ValidFrom,
                ValidTo = entity.ValidTo
            };
    }

    AddCompanyUserDto? IBaseMapper<AddCompanyUserDto, CompanyMembershipAddRequest>.Map(
        CompanyMembershipAddRequest? entity)
    {
        return entity is null
            ? null
            : new AddCompanyUserDto
            {
                Email = entity.Email,
                RoleId = entity.RoleId,
                JobTitle = entity.JobTitle,
                ValidFrom = entity.ValidFrom,
                ValidTo = entity.ValidTo
            };
    }

    UpdateCompanyUserDto? IBaseMapper<UpdateCompanyUserDto, CompanyMembershipUpdateRequest>.Map(
        CompanyMembershipUpdateRequest? entity)
    {
        return entity is null
            ? null
            : new UpdateCompanyUserDto
            {
                RoleId = entity.RoleId,
                JobTitle = entity.JobTitle,
                ValidFrom = entity.ValidFrom,
                ValidTo = entity.ValidTo
            };
    }
}
