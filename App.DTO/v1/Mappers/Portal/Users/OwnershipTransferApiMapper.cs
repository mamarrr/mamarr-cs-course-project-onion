using App.BLL.DTO.ManagementCompanies.Models;
using App.DTO.v1.Portal.Users;
using Base.Contracts;

namespace App.DTO.v1.Mappers.Portal.Users;

public sealed class OwnershipTransferApiMapper :
    IBaseMapper<TransferOwnershipDto, TransferOwnershipRequest>
{
    public OwnershipTransferCandidateDto? Map(OwnershipTransferCandidate? entity)
    {
        return entity is null
            ? null
            : new OwnershipTransferCandidateDto
            {
                MembershipId = entity.MembershipId,
                AppUserId = entity.AppUserId,
                FullName = entity.FullName,
                Email = entity.Email,
                RoleId = entity.RoleId,
                RoleCode = entity.RoleCode,
                RoleLabel = entity.RoleLabel,
                IsEffective = entity.IsEffective
            };
    }

    public OwnershipTransferResultDto? Map(OwnershipTransferModel? entity)
    {
        return entity is null
            ? null
            : new OwnershipTransferResultDto
            {
                PreviousOwnerMembershipId = entity.PreviousOwnerMembershipId,
                NewOwnerMembershipId = entity.NewOwnerMembershipId
            };
    }

    public TransferOwnershipRequest? Map(TransferOwnershipDto? entity)
    {
        return entity is null
            ? null
            : new TransferOwnershipRequest
            {
                TargetMembershipId = entity.TargetMembershipId
            };
    }

    TransferOwnershipDto? IBaseMapper<TransferOwnershipDto, TransferOwnershipRequest>.Map(
        TransferOwnershipRequest? entity)
    {
        return entity is null
            ? null
            : new TransferOwnershipDto
            {
                TargetMembershipId = entity.TargetMembershipId
            };
    }
}
