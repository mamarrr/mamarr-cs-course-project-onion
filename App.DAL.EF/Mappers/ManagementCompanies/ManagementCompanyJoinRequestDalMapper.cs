using App.Contracts.DAL.ManagementCompanies;
using App.Domain;
using Base.Contracts;
using Base.Domain;

namespace App.DAL.EF.Mappers.ManagementCompanies;

public sealed class ManagementCompanyJoinRequestDalMapper :
    IMapper<ManagementCompanyJoinRequestDalDto, ManagementCompanyJoinRequest>
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
            RequesterFirstName = entity.AppUser?.FirstName ?? string.Empty,
            RequesterLastName = entity.AppUser?.LastName ?? string.Empty,
            RequesterEmail = entity.AppUser?.Email ?? string.Empty,
            ManagementCompanyId = entity.ManagementCompanyId,
            RequestedRoleId = entity.RequestedManagementCompanyRoleId,
            RequestedRoleCode = entity.RequestedManagementCompanyRole?.Code ?? string.Empty,
            RequestedRoleLabel = entity.RequestedManagementCompanyRole?.Label.ToString() ?? string.Empty,
            StatusId = entity.ManagementCompanyJoinRequestStatusId,
            StatusCode = entity.ManagementCompanyJoinRequestStatus?.Code ?? string.Empty,
            StatusLabel = entity.ManagementCompanyJoinRequestStatus?.Label.ToString() ?? string.Empty,
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
            Message = string.IsNullOrWhiteSpace(entity.Message) ? null : new LangStr(entity.Message),
            CreatedAt = entity.CreatedAt,
            ResolvedAt = entity.ResolvedAt,
            ResolvedByAppUserId = entity.ResolvedByAppUserId
        };
    }
}
