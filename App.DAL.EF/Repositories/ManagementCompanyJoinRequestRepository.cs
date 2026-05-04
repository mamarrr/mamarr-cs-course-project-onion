using App.DAL.Contracts.Repositories;
using App.DAL.DTO.ManagementCompanies;
using App.DAL.EF.Mappers.ManagementCompanies;
using App.Domain;
using Base.Domain;
using Base.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class ManagementCompanyJoinRequestRepository :
    BaseRepository<ManagementCompanyJoinRequestDalDto, ManagementCompanyJoinRequest, AppDbContext>,
    IManagementCompanyJoinRequestRepository
{
    private readonly AppDbContext _dbContext;

    public ManagementCompanyJoinRequestRepository(
        AppDbContext dbContext,
        ManagementCompanyJoinRequestDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ManagementCompanyJoinRequestDetailsDalDto>> PendingByCompanyAsync(
        Guid managementCompanyId,
        Guid pendingStatusId,
        CancellationToken cancellationToken = default)
    {
        return await RequestQuery()
            .Where(request => request.ManagementCompanyId == managementCompanyId
                              && request.ManagementCompanyJoinRequestStatusId == pendingStatusId)
            .OrderByDescending(request => request.CreatedAt)
            .Select(request => new ManagementCompanyJoinRequestDetailsDalDto
            {
                Id = request.Id,
                AppUserId = request.AppUserId,
                RequesterFirstName = request.AppUser == null ? string.Empty : request.AppUser.FirstName,
                RequesterLastName = request.AppUser == null ? string.Empty : request.AppUser.LastName,
                RequesterEmail = request.AppUser == null ? string.Empty : request.AppUser.Email ?? string.Empty,
                ManagementCompanyId = request.ManagementCompanyId,
                RequestedRoleId = request.RequestedManagementCompanyRoleId,
                RequestedRoleCode = request.RequestedManagementCompanyRole == null ? string.Empty : request.RequestedManagementCompanyRole.Code,
                RequestedRoleLabel = request.RequestedManagementCompanyRole == null ? string.Empty : request.RequestedManagementCompanyRole.Label.ToString(),
                StatusId = request.ManagementCompanyJoinRequestStatusId,
                StatusCode = request.ManagementCompanyJoinRequestStatus == null ? string.Empty : request.ManagementCompanyJoinRequestStatus.Code,
                StatusLabel = request.ManagementCompanyJoinRequestStatus == null ? string.Empty : request.ManagementCompanyJoinRequestStatus.Label.ToString(),
                Message = request.Message == null ? null : request.Message.ToString(),
                CreatedAt = request.CreatedAt,
                ResolvedAt = request.ResolvedAt,
                ResolvedByAppUserId = request.ResolvedByAppUserId
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ManagementCompanyJoinRequestDetailsDalDto?> FindByIdAndCompanyAsync(
        Guid requestId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await RequestQuery()
            .Where(request => request.Id == requestId
                              && request.ManagementCompanyId == managementCompanyId)
            .Select(request => new ManagementCompanyJoinRequestDetailsDalDto
            {
                Id = request.Id,
                AppUserId = request.AppUserId,
                RequesterFirstName = request.AppUser == null ? string.Empty : request.AppUser.FirstName,
                RequesterLastName = request.AppUser == null ? string.Empty : request.AppUser.LastName,
                RequesterEmail = request.AppUser == null ? string.Empty : request.AppUser.Email ?? string.Empty,
                ManagementCompanyId = request.ManagementCompanyId,
                RequestedRoleId = request.RequestedManagementCompanyRoleId,
                RequestedRoleCode = request.RequestedManagementCompanyRole == null ? string.Empty : request.RequestedManagementCompanyRole.Code,
                RequestedRoleLabel = request.RequestedManagementCompanyRole == null ? string.Empty : request.RequestedManagementCompanyRole.Label.ToString(),
                StatusId = request.ManagementCompanyJoinRequestStatusId,
                StatusCode = request.ManagementCompanyJoinRequestStatus == null ? string.Empty : request.ManagementCompanyJoinRequestStatus.Code,
                StatusLabel = request.ManagementCompanyJoinRequestStatus == null ? string.Empty : request.ManagementCompanyJoinRequestStatus.Label.ToString(),
                Message = request.Message == null ? null : request.Message.ToString(),
                CreatedAt = request.CreatedAt,
                ResolvedAt = request.ResolvedAt,
                ResolvedByAppUserId = request.ResolvedByAppUserId
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasPendingRequestAsync(
        Guid appUserId,
        Guid managementCompanyId,
        Guid pendingStatusId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ManagementCompanyJoinRequests
            .AsNoTracking()
            .AnyAsync(
                request => request.AppUserId == appUserId
                           && request.ManagementCompanyId == managementCompanyId
                           && request.ManagementCompanyJoinRequestStatusId == pendingStatusId,
                cancellationToken);
    }

    public void AddJoinRequest(ManagementCompanyJoinRequestCreateDalDto dto)
    {
        _dbContext.ManagementCompanyJoinRequests.Add(new ManagementCompanyJoinRequest
        {
            Id = dto.Id,
            AppUserId = dto.AppUserId,
            ManagementCompanyId = dto.ManagementCompanyId,
            RequestedManagementCompanyRoleId = dto.RequestedRoleId,
            ManagementCompanyJoinRequestStatusId = dto.StatusId,
            Message = string.IsNullOrWhiteSpace(dto.Message) ? null : new LangStr(dto.Message.Trim()),
            CreatedAt = dto.CreatedAt
        });
    }

    public async Task<bool> SetStatusAsync(
        Guid requestId,
        Guid managementCompanyId,
        Guid statusId,
        Guid resolvedByAppUserId,
        DateTime resolvedAt,
        CancellationToken cancellationToken = default)
    {
        var request = await _dbContext.ManagementCompanyJoinRequests
            .AsTracking()
            .FirstOrDefaultAsync(
                request => request.Id == requestId
                           && request.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (request is null)
        {
            return false;
        }

        request.ManagementCompanyJoinRequestStatusId = statusId;
        request.ResolvedAt = resolvedAt;
        request.ResolvedByAppUserId = resolvedByAppUserId;
        return true;
    }

    private IQueryable<ManagementCompanyJoinRequest> RequestQuery()
    {
        return _dbContext.ManagementCompanyJoinRequests
            .AsNoTracking()
            .Include(request => request.AppUser)
            .Include(request => request.RequestedManagementCompanyRole)
            .Include(request => request.ManagementCompanyJoinRequestStatus);
    }
}
