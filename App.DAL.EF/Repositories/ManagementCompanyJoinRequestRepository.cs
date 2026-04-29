using App.Contracts.DAL.ManagementCompanies;
using App.DAL.EF.Mappers.ManagementCompanies;
using App.Domain;
using Base.Domain;
using Base.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public sealed class ManagementCompanyJoinRequestRepository :
    BaseRepository<ManagementCompanyJoinRequestDalDto, ManagementCompanyJoinRequest, AppDbContext>,
    IManagementCompanyJoinRequestRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ManagementCompanyJoinRequestDalMapper _mapper;

    public ManagementCompanyJoinRequestRepository(
        AppDbContext dbContext,
        ManagementCompanyJoinRequestDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ManagementCompanyJoinRequestDalDto>> PendingByCompanyAsync(
        Guid managementCompanyId,
        Guid pendingStatusId,
        CancellationToken cancellationToken = default)
    {
        var requests = await RequestQuery()
            .Where(request => request.ManagementCompanyId == managementCompanyId
                              && request.ManagementCompanyJoinRequestStatusId == pendingStatusId)
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken);

        return requests.Select(_mapper.Map).OfType<ManagementCompanyJoinRequestDalDto>().ToList();
    }

    public async Task<ManagementCompanyJoinRequestDalDto?> FindByIdAndCompanyAsync(
        Guid requestId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var request = await RequestQuery()
            .FirstOrDefaultAsync(
                request => request.Id == requestId
                           && request.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        return _mapper.Map(request);
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
