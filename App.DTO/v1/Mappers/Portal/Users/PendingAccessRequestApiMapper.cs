using App.BLL.DTO.ManagementCompanies.Models;
using App.DTO.v1.Portal.Users;

namespace App.DTO.v1.Mappers.Portal.Users;

public sealed class PendingAccessRequestApiMapper
{
    public PendingAccessRequestDto? Map(PendingAccessRequestItem? entity)
    {
        return entity is null
            ? null
            : new PendingAccessRequestDto
            {
                RequestId = entity.RequestId,
                AppUserId = entity.AppUserId,
                RequesterName = entity.RequesterName,
                RequesterEmail = entity.RequesterEmail,
                RequestedRoleCode = entity.RequestedRoleCode,
                RequestedRoleLabel = entity.RequestedRoleLabel,
                Message = entity.Message,
                RequestedAt = entity.RequestedAt
            };
    }
}
