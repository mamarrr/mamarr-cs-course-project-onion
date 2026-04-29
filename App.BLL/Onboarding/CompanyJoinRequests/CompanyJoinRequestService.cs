using System.Globalization;
using System.Text.Json;
using App.DAL.EF;
using App.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.BLL.Onboarding.CompanyJoinRequests;

public class CompanyJoinRequestService : ICompanyJoinRequestService
{
    private const string PendingStatusCode = "PENDING";
    private const string ApprovedStatusCode = "APPROVED";
    private const string RejectedStatusCode = "REJECTED";

    private static readonly HashSet<string> ApproverRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER"
    };

    private readonly AppDbContext _dbContext;
    private readonly ILogger<CompanyJoinRequestService> _logger;

    public CompanyJoinRequestService(
        AppDbContext dbContext,
        ILogger<CompanyJoinRequestService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CompanyJoinRequestResult> CreateJoinRequestAsync(
        CompanyJoinRequest request,
        CancellationToken cancellationToken = default)
    {
        var registryCode = request.RegistryCode.Trim();
        if (registryCode.Length == 0)
        {
            _logger.LogWarning("Join request validation failed: empty registry code. AppUserId={AppUserId}", request.AppUserId);
            return new CompanyJoinRequestResult
            {
                UnknownRegistryCode = true,
                ErrorMessage = L("ManagementCompanyWasNotFound", "Management company was not found.")
            };
        }

        var company = await _dbContext.ManagementCompanies
            .AsNoTracking()
            .Where(x => x.IsActive)
            .SingleOrDefaultAsync(x => x.RegistryCode == registryCode, cancellationToken);

        if (company == null)
        {
            _logger.LogWarning("Join request validation failed: company not found by registry code. AppUserId={AppUserId}; RegistryCode={RegistryCode}", request.AppUserId, registryCode);
            return new CompanyJoinRequestResult
            {
                UnknownRegistryCode = true,
                ErrorMessage = L("ManagementCompanyWasNotFound", "Management company was not found.")
            };
        }

        var roleExists = await _dbContext.ManagementCompanyRoles
            .AsNoTracking()
            .AnyAsync(x => x.Id == request.RequestedRoleId, cancellationToken);

        if (!roleExists)
        {
            _logger.LogWarning("Join request validation failed: requested role does not exist. AppUserId={AppUserId}; RequestedRoleId={RequestedRoleId}", request.AppUserId, request.RequestedRoleId);
            return new CompanyJoinRequestResult
            {
                InvalidRole = true,
                ErrorMessage = L("SelectedRoleIsInvalid", "Selected role is invalid.")
            };
        }

        var membershipExists = await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .AnyAsync(x => x.AppUserId == request.AppUserId
                           && x.ManagementCompanyId == company.Id,
                cancellationToken);

        if (membershipExists)
        {
            _logger.LogInformation("Join request skipped: user is already a member. AppUserId={AppUserId}; ManagementCompanyId={ManagementCompanyId}", request.AppUserId, company.Id);
            return new CompanyJoinRequestResult
            {
                AlreadyMember = true,
                ErrorMessage = L("AlreadyMemberOfThisManagementCompany", "You are already a member of this management company.")
            };
        }

        var pendingStatusId = await GetStatusIdAsync(PendingStatusCode, cancellationToken);

        var duplicatePending = await _dbContext.ManagementCompanyJoinRequests
            .AsNoTracking()
            .AnyAsync(x => x.AppUserId == request.AppUserId
                           && x.ManagementCompanyId == company.Id
                           && x.ManagementCompanyJoinRequestStatusId == pendingStatusId,
                cancellationToken);

        if (duplicatePending)
        {
            _logger.LogInformation("Join request skipped: duplicate pending request exists. AppUserId={AppUserId}; ManagementCompanyId={ManagementCompanyId}", request.AppUserId, company.Id);
            return new CompanyJoinRequestResult
            {
                DuplicatePendingRequest = true,
                ErrorMessage = L("PendingRequestForThisCompanyAlreadyExists", "A pending request for this company already exists.")
            };
        }

        var joinRequest = new ManagementCompanyJoinRequest
        {
            Id = Guid.NewGuid(),
            AppUserId = request.AppUserId,
            ManagementCompanyId = company.Id,
            RequestedManagementCompanyRoleId = request.RequestedRoleId,
            ManagementCompanyJoinRequestStatusId = pendingStatusId,
            Message = string.IsNullOrWhiteSpace(request.Message) ? null : new Base.Domain.LangStr(request.Message.Trim()),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ManagementCompanyJoinRequests.Add(joinRequest);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _logger.LogWarning("Join request save failed with DbUpdateException; returning duplicate pending response. AppUserId={AppUserId}; ManagementCompanyId={ManagementCompanyId}", request.AppUserId, company.Id);
            return new CompanyJoinRequestResult
            {
                DuplicatePendingRequest = true,
                ErrorMessage = L("PendingRequestForThisCompanyAlreadyExists", "A pending request for this company already exists.")
            };
        }

        return new CompanyJoinRequestResult
        {
            Success = true,
            RequestId = joinRequest.Id
        };
    }

    public async Task<IReadOnlyList<CompanyJoinRequestListItem>> ListPendingForCompanyAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var pendingStatusId = await GetStatusIdAsync(PendingStatusCode, cancellationToken);

        var requests = await _dbContext.ManagementCompanyJoinRequests
            .AsNoTracking()
            .Where(x => x.ManagementCompanyId == managementCompanyId
                        && x.ManagementCompanyJoinRequestStatusId == pendingStatusId)
            .Include(x => x.AppUser)
            .Include(x => x.RequestedManagementCompanyRole)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return requests
            .Select(x => new CompanyJoinRequestListItem
            {
                RequestId = x.Id,
                AppUserId = x.AppUserId,
                RequesterName = $"{x.AppUser!.FirstName} {x.AppUser.LastName}".Trim(),
                RequesterEmail = x.AppUser!.Email ?? string.Empty,
                RequestedRoleId = x.RequestedManagementCompanyRoleId,
                RequestedRoleCode = x.RequestedManagementCompanyRole!.Code,
                RequestedRoleLabel = x.RequestedManagementCompanyRole!.Label.ToString(),
                Message = NormalizePossiblySerializedLangStr(x.Message),
                CreatedAt = x.CreatedAt
            })
            .ToList();
    }

    private static string? NormalizePossiblySerializedLangStr(Base.Domain.LangStr? value)
    {
        if (value == null)
        {
            return null;
        }

        var localized = value.ToString();
        var trimmed = localized.Trim();
        if (!trimmed.StartsWith("{", StringComparison.Ordinal))
        {
            return localized;
        }

        try
        {
            var nested = JsonSerializer.Deserialize<Base.Domain.LangStr>(trimmed, (JsonSerializerOptions?)null);
            if (nested == null)
            {
                return localized;
            }

            var nestedLocalized = nested.ToString();
            return string.IsNullOrWhiteSpace(nestedLocalized) ? localized : nestedLocalized;
        }
        catch
        {
            return localized;
        }
    }

    public Task<ResolveCompanyJoinRequestResult> ApproveRequestAsync(
        Guid actorAppUserId,
        Guid managementCompanyId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        return ResolveAsync(
            actorAppUserId,
            managementCompanyId,
            requestId,
            ApprovedStatusCode,
            createMembership: true,
            cancellationToken);
    }

    public Task<ResolveCompanyJoinRequestResult> RejectRequestAsync(
        Guid actorAppUserId,
        Guid managementCompanyId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        return ResolveAsync(
            actorAppUserId,
            managementCompanyId,
            requestId,
            RejectedStatusCode,
            createMembership: false,
            cancellationToken);
    }

    private async Task<ResolveCompanyJoinRequestResult> ResolveAsync(
        Guid actorAppUserId,
        Guid managementCompanyId,
        Guid requestId,
        string targetStatusCode,
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
            return new ResolveCompanyJoinRequestResult
            {
                Forbidden = true,
                ErrorMessage = L("NoPermissionToResolveAccessRequests", "You do not have permission to resolve access requests.")
            };
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var pendingStatusId = await GetStatusIdAsync(PendingStatusCode, cancellationToken);
        var targetStatusId = await GetStatusIdAsync(targetStatusCode, cancellationToken);

        var joinRequest = await _dbContext.ManagementCompanyJoinRequests
            .AsTracking()
            .SingleOrDefaultAsync(x => x.Id == requestId
                                       && x.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (joinRequest == null)
        {
            return new ResolveCompanyJoinRequestResult
            {
                NotFound = true,
                ErrorMessage = L("JoinRequestNotFound", "Join request not found.")
            };
        }

        if (joinRequest.ManagementCompanyJoinRequestStatusId != pendingStatusId)
        {
            return new ResolveCompanyJoinRequestResult
            {
                AlreadyResolved = true,
                ErrorMessage = L("JoinRequestAlreadyResolved", "Join request is already resolved.")
            };
        }

        var requesterMembershipExists = await _dbContext.ManagementCompanyUsers
            .AsNoTracking()
            .AnyAsync(x => x.AppUserId == joinRequest.AppUserId
                           && x.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (requesterMembershipExists)
        {
            return new ResolveCompanyJoinRequestResult
            {
                AlreadyMember = true,
                ErrorMessage = L("RequesterAlreadyMemberOfThisCompany", "Requester is already a member of this company.")
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

        joinRequest.ManagementCompanyJoinRequestStatusId = targetStatusId;
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
            return new ResolveCompanyJoinRequestResult
            {
                AlreadyMember = true,
                ErrorMessage = L("RequesterAlreadyMemberOfThisCompany", "Requester is already a member of this company.")
            };
        }

        return new ResolveCompanyJoinRequestResult
        {
            Success = true
        };
    }

    private async Task<Guid> GetStatusIdAsync(string code, CancellationToken cancellationToken)
    {
        var statusId = await _dbContext.ManagementCompanyJoinRequestStatuses
            .AsNoTracking()
            .Where(x => x.Code == code)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return statusId ?? throw new InvalidOperationException($"Management company join request status '{code}' is not seeded.");
    }

    private static string L(string resourceKey, string fallback)
    {
        return App.Resources.Views.UiText.ResourceManager.GetString(resourceKey, CultureInfo.CurrentUICulture) ?? fallback;
    }
}

