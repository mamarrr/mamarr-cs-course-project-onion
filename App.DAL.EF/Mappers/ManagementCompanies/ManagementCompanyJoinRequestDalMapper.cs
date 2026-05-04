using App.DAL.DTO.ManagementCompanies;
using App.Domain;
using Base.Contracts;
using Base.Domain;

namespace App.DAL.EF.Mappers.ManagementCompanies;

public class ManagementCompanyJoinRequestDalMapper :
    IBaseMapper<ManagementCompanyJoinRequestDalDto, ManagementCompanyJoinRequest>
{
    public ManagementCompanyJoinRequestDalDto? Map(ManagementCompanyJoinRequest? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new ManagementCompanyJoinRequestDalDto
        {
            Id = entity.Id,
            AppUserId = entity.AppUserId,
            ManagementCompanyId = entity.ManagementCompanyId,
            RequestedRoleId = entity.RequestedManagementCompanyRoleId,
            StatusId = entity.ManagementCompanyJoinRequestStatusId,
            Message = entity.Message?.ToString(),
            CreatedAt = entity.CreatedAt,
            ResolvedAt = entity.ResolvedAt,
            ResolvedByAppUserId = entity.ResolvedByAppUserId
        };
    }

    public ManagementCompanyJoinRequest? Map(ManagementCompanyJoinRequestDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new ManagementCompanyJoinRequest
        {
            Id = entity.Id,
            AppUserId = entity.AppUserId,
            ManagementCompanyId = entity.ManagementCompanyId,
            RequestedManagementCompanyRoleId = entity.RequestedRoleId,
            ManagementCompanyJoinRequestStatusId = entity.StatusId,
            Message = string.IsNullOrWhiteSpace(entity.Message) ? null : new LangStr(entity.Message.Trim()),
            CreatedAt = entity.CreatedAt,
            ResolvedAt = entity.ResolvedAt,
            ResolvedByAppUserId = entity.ResolvedByAppUserId
        };
    }
}
