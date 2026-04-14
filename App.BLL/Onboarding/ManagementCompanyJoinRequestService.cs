using App.DAL.EF;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Onboarding;

public class ManagementCompanyJoinRequestService : IManagementCompanyJoinRequestService
{
    private static readonly HashSet<string> ApproverRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER"
    };

    private readonly AppDbContext _dbContext;

    public ManagementCompanyJoinRequestService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateManagementCompanyJoinRequestResult> CreateJoinRequestAsync(
        CreateManagementCompanyJoinRequest request,
        CancellationToken cancellationToken = default)
    {
        var registryCode = request.RegistryCode.Trim();
        if (registryCode.Length == 0)
        {
            return new CreateManagementCompanyJoinRequestResult
            {
                UnknownRegistryCode = true,
                ErrorMessage = "Management company was not found."
            };
        }

        var company = await _dbContext.ManagementCompanies
            .AsNoTracking()
            .Where(x => x.IsActive)
            .SingleOrDefaultAsync(x => x.RegistryCode == registryCode, cancellationToken);

        if (company == null)
        {
            return new CreateManagementCompanyJoinRequestResult
            {
                UnknownRegistryCode = true,
                ErrorMessage = "Management company was not found."
            };
        }

        var roleExists = await _dbContext.ManagementCompanyRoles
            .AsNoTracking()
            .AnyAsync(x => x.Id == request.RequestedRoleId, cancellationToken);

        if (!roleExists)
        {
            return new CreateManagementCompanyJoinRequestResult
            {
                InvalidRole = true,
                ErrorMessage = "Selected role is invalid."
            };
        }

        var membershipExists = await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .AnyAsync(x => x.AppUserId == request.AppUserId
                           && x.ManagementCompanyId == company.Id,
                cancellationToken);

        if (membershipExists)
        {
            return new CreateManagementCompanyJoinRequestResult
            {
                AlreadyMember = true,
                ErrorMessage = "You are already a member of this management company."
            };
        }

        var duplicatePending = await _dbContext.ManagementCompanyJoinRequests
            .AsNoTracking()
            .AnyAsync(x => x.AppUserId == request.AppUserId
                           && x.ManagementCompanyId == company.Id
                           && x.Status == ManagementCompanyJoinRequestStatus.Pending,
                cancellationToken);

        if (duplicatePending)
        {
            return new CreateManagementCompanyJoinRequestResult
            {
                DuplicatePendingRequest = true,
                ErrorMessage = "A pending request for this company already exists."
            };
        }

        var joinRequest = new ManagementCompanyJoinRequest
        {
            Id = Guid.NewGuid(),
            AppUserId = request.AppUserId,
            ManagementCompanyId = company.Id,
            RequestedManagementCompanyRoleId = request.RequestedRoleId,
            Status = ManagementCompanyJoinRequestStatus.Pending,
            Message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ManagementCompanyJoinRequests.Add(joinRequest);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new CreateManagementCompanyJoinRequestResult
            {
                DuplicatePendingRequest = true,
                ErrorMessage = "A pending request for this company already exists."
            };
        }

        return new CreateManagementCompanyJoinRequestResult
        {
            Success = true,
            RequestId = joinRequest.Id
        };
    }

    public async Task<IReadOnlyList<ManagementCompanyJoinRequestListItem>> ListPendingForCompanyAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ManagementCompanyJoinRequests
            .AsNoTracking()
            .Where(x => x.ManagementCompanyId == managementCompanyId
                        && x.Status == ManagementCompanyJoinRequestStatus.Pending)
            .Include(x => x.AppUser)
            .Include(x => x.RequestedManagementCompanyRole)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ManagementCompanyJoinRequestListItem
            {
                RequestId = x.Id,
                AppUserId = x.AppUserId,
                RequesterName = $"{x.AppUser!.FirstName} {x.AppUser.LastName}".Trim(),
                RequesterEmail = x.AppUser!.Email ?? string.Empty,
                RequestedRoleId = x.RequestedManagementCompanyRoleId,
                RequestedRoleCode = x.RequestedManagementCompanyRole!.Code,
                RequestedRoleLabel = x.RequestedManagementCompanyRole!.Label.ToString(),
                Message = x.Message,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public Task<ResolveManagementCompanyJoinRequestResult> ApproveRequestAsync(
        Guid actorAppUserId,
        Guid managementCompanyId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        return ResolveAsync(
            actorAppUserId,
            managementCompanyId,
            requestId,
            ManagementCompanyJoinRequestStatus.Approved,
            createMembership: true,
            cancellationToken);
    }

    public Task<ResolveManagementCompanyJoinRequestResult> RejectRequestAsync(
        Guid actorAppUserId,
        Guid managementCompanyId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        return ResolveAsync(
            actorAppUserId,
            managementCompanyId,
            requestId,
            ManagementCompanyJoinRequestStatus.Rejected,
            createMembership: false,
            cancellationToken);
    }

    private async Task<ResolveManagementCompanyJoinRequestResult> ResolveAsync(
        Guid actorAppUserId,
        Guid managementCompanyId,
        Guid requestId,
        string targetStatus,
        bool createMembership,
        CancellationToken cancellationToken)
    {
        var actorMembership = await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .Where(x => x.AppUserId == actorAppUserId
                        && x.ManagementCompanyId == managementCompanyId
                        && x.IsActive)
            .Include(x => x.ManagementCompanyRole)
            .SingleOrDefaultAsync(cancellationToken);

        if (actorMembership == null
            || actorMembership.ManagementCompanyRole == null
            || !ApproverRoleCodes.Contains(actorMembership.ManagementCompanyRole.Code))
        {
            return new ResolveManagementCompanyJoinRequestResult
            {
                Forbidden = true,
                ErrorMessage = "You do not have permission to resolve access requests."
            };
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var joinRequest = await _dbContext.ManagementCompanyJoinRequests
            .AsTracking()
            .SingleOrDefaultAsync(x => x.Id == requestId
                                       && x.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (joinRequest == null)
        {
            return new ResolveManagementCompanyJoinRequestResult
            {
                NotFound = true,
                ErrorMessage = "Join request not found."
            };
        }

        if (joinRequest.Status != ManagementCompanyJoinRequestStatus.Pending)
        {
            return new ResolveManagementCompanyJoinRequestResult
            {
                AlreadyResolved = true,
                ErrorMessage = "Join request is already resolved."
            };
        }

        var requesterMembershipExists = await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .AnyAsync(x => x.AppUserId == joinRequest.AppUserId
                           && x.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (requesterMembershipExists)
        {
            return new ResolveManagementCompanyJoinRequestResult
            {
                AlreadyMember = true,
                ErrorMessage = "Requester is already a member of this company."
            };
        }

        if (createMembership)
        {
            var membership = new ManagementCompanyUser
            {
                Id = Guid.NewGuid(),
                ManagementCompanyId = managementCompanyId,
                AppUserId = joinRequest.AppUserId,
                ManagementCompanyRoleId = joinRequest.RequestedManagementCompanyRoleId,
                JobTitle = "Employee",
                IsActive = true,
                ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.ManagementCompanyUsers.Add(membership);
        }

        joinRequest.Status = targetStatus;
        joinRequest.ResolvedAt = DateTime.UtcNow;
        joinRequest.ResolvedByAppUserId = actorAppUserId;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(cancellationToken);
            return new ResolveManagementCompanyJoinRequestResult
            {
                AlreadyMember = true,
                ErrorMessage = "Requester is already a member of this company."
            };
        }

        return new ResolveManagementCompanyJoinRequestResult
        {
            Success = true
        };
    }
}

