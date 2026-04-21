using System.Globalization;
using App.BLL.ResidentWorkspace.Residents;
using App.BLL.UnitWorkspace.Workspace;
using App.DAL.EF;
using App.Domain;
using Base.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.LeaseAssignments;

public class ManagementLeaseService : IManagementLeaseService
{
    private readonly AppDbContext _dbContext;

    public ManagementLeaseService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ManagementResidentLeaseListResult> ListForResidentAsync(
        ResidentDashboardContext context,
        CancellationToken cancellationToken = default)
    {
        var leases = await _dbContext.Leases
            .AsNoTracking()
            .Where(l => l.ResidentId == context.ResidentId)
            .Where(l => l.Resident!.ManagementCompanyId == context.ManagementCompanyId)
            .OrderByDescending(l => l.IsActive)
            .ThenByDescending(l => l.StartDate)
            .ThenBy(l => l.EndDate)
            .Select(l => new ManagementResidentLeaseListItem
            {
                LeaseId = l.Id,
                ResidentId = l.ResidentId,
                UnitId = l.UnitId,
                PropertyId = l.Unit!.PropertyId,
                PropertyName = l.Unit.Property!.Label.ToString(),
                PropertySlug = l.Unit.Property.Slug,
                UnitNr = l.Unit.UnitNr,
                UnitSlug = l.Unit.Slug,
                LeaseRoleId = l.LeaseRoleId,
                LeaseRoleCode = l.LeaseRole!.Code,
                LeaseRoleLabel = l.LeaseRole.Label.ToString(),
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                IsActive = l.IsActive,
                Notes = l.Notes == null ? null : l.Notes.ToString()
            })
            .ToListAsync(cancellationToken);

        return new ManagementResidentLeaseListResult
        {
            Leases = leases
        };
    }

    public async Task<ManagementUnitLeaseListResult> ListForUnitAsync(
        UnitDashboardContext context,
        CancellationToken cancellationToken = default)
    {
        var leases = await _dbContext.Leases
            .AsNoTracking()
            .Where(l => l.UnitId == context.UnitId)
            .Where(l => l.Resident!.ManagementCompanyId == context.ManagementCompanyId)
            .Where(l => l.Unit!.PropertyId == context.PropertyId)
            .OrderByDescending(l => l.IsActive)
            .ThenByDescending(l => l.StartDate)
            .ThenBy(l => l.EndDate)
            .Select(l => new ManagementUnitLeaseListItem
            {
                LeaseId = l.Id,
                ResidentId = l.ResidentId,
                UnitId = l.UnitId,
                PropertyId = l.Unit!.PropertyId,
                ResidentFullName = string.Join(" ", new[] { l.Resident!.FirstName, l.Resident.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))),
                ResidentIdCode = l.Resident!.IdCode,
                LeaseRoleId = l.LeaseRoleId,
                LeaseRoleCode = l.LeaseRole!.Code,
                LeaseRoleLabel = l.LeaseRole.Label.ToString(),
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                IsActive = l.IsActive,
                Notes = l.Notes == null ? null : l.Notes.ToString()
            })
            .ToListAsync(cancellationToken);

        return new ManagementUnitLeaseListResult
        {
            Leases = leases
        };
    }

    public Task<ManagementLeaseDetailsResult> GetForResidentAsync(
        ResidentDashboardContext context,
        Guid leaseId,
        CancellationToken cancellationToken = default)
    {
        return GetLeaseAsync(
            query => query.Where(l => l.Id == leaseId && l.ResidentId == context.ResidentId && l.Resident!.ManagementCompanyId == context.ManagementCompanyId),
            cancellationToken);
    }

    public Task<ManagementLeaseDetailsResult> GetForUnitAsync(
        UnitDashboardContext context,
        Guid leaseId,
        CancellationToken cancellationToken = default)
    {
        return GetLeaseAsync(
            query => query.Where(l => l.Id == leaseId && l.UnitId == context.UnitId && l.Unit!.PropertyId == context.PropertyId && l.Resident!.ManagementCompanyId == context.ManagementCompanyId),
            cancellationToken);
    }

    public async Task<ManagementLeaseCommandResult> CreateFromResidentAsync(
        ResidentDashboardContext context,
        ManagementLeaseCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateCreateFromResidentAsync(context, request, cancellationToken);
        if (!validation.Success)
        {
            return validation.Result;
        }

        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            LeaseRoleId = request.LeaseRoleId,
            ResidentId = context.ResidentId,
            UnitId = validation.UnitId!.Value,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = request.IsActive,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : new LangStr(request.Notes.Trim())
        };

        _dbContext.Leases.Add(lease);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ManagementLeaseCommandResult
        {
            Success = true,
            LeaseId = lease.Id
        };
    }

    public async Task<ManagementLeaseCommandResult> CreateFromUnitAsync(
        UnitDashboardContext context,
        ManagementLeaseCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateCreateFromUnitAsync(context, request, cancellationToken);
        if (!validation.Success)
        {
            return validation.Result;
        }

        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            LeaseRoleId = request.LeaseRoleId,
            ResidentId = validation.ResidentId!.Value,
            UnitId = context.UnitId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = request.IsActive,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : new LangStr(request.Notes.Trim())
        };

        _dbContext.Leases.Add(lease);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ManagementLeaseCommandResult
        {
            Success = true,
            LeaseId = lease.Id
        };
    }

    public async Task<ManagementLeaseCommandResult> UpdateFromResidentAsync(
        ResidentDashboardContext context,
        ManagementLeaseUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateUpdateFromResidentAsync(context, request, cancellationToken);
        if (!validation.Success || validation.Lease == null)
        {
            return validation.Result;
        }

        ApplyUpdate(validation.Lease, request);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ManagementLeaseCommandResult
        {
            Success = true,
            LeaseId = validation.Lease.Id
        };
    }

    public async Task<ManagementLeaseCommandResult> UpdateFromUnitAsync(
        UnitDashboardContext context,
        ManagementLeaseUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateUpdateFromUnitAsync(context, request, cancellationToken);
        if (!validation.Success || validation.Lease == null)
        {
            return validation.Result;
        }

        ApplyUpdate(validation.Lease, request);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ManagementLeaseCommandResult
        {
            Success = true,
            LeaseId = validation.Lease.Id
        };
    }

    public async Task<ManagementLeaseCommandResult> DeleteFromResidentAsync(
        ResidentDashboardContext context,
        ManagementLeaseDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        var lease = await _dbContext.Leases
            .FirstOrDefaultAsync(
                l => l.Id == request.LeaseId && l.ResidentId == context.ResidentId && l.Resident!.ManagementCompanyId == context.ManagementCompanyId,
                cancellationToken);

        if (lease == null)
        {
            return new ManagementLeaseCommandResult
            {
                LeaseNotFound = true,
                ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("LeaseWasNotFound") ?? "Lease was not found."
            };
        }

        _dbContext.Leases.Remove(lease);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ManagementLeaseCommandResult
        {
            Success = true,
            LeaseId = lease.Id
        };
    }

    public async Task<ManagementLeaseCommandResult> DeleteFromUnitAsync(
        UnitDashboardContext context,
        ManagementLeaseDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        var lease = await _dbContext.Leases
            .FirstOrDefaultAsync(
                l => l.Id == request.LeaseId && l.UnitId == context.UnitId && l.Unit!.PropertyId == context.PropertyId && l.Resident!.ManagementCompanyId == context.ManagementCompanyId,
                cancellationToken);

        if (lease == null)
        {
            return new ManagementLeaseCommandResult
            {
                LeaseNotFound = true,
                ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("LeaseWasNotFound") ?? "Lease was not found."
            };
        }

        _dbContext.Leases.Remove(lease);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ManagementLeaseCommandResult
        {
            Success = true,
            LeaseId = lease.Id
        };
    }

    private async Task<ManagementLeaseDetailsResult> GetLeaseAsync(
        Func<IQueryable<Lease>, IQueryable<Lease>> scope,
        CancellationToken cancellationToken)
    {
        var lease = await scope(_dbContext.Leases.AsNoTracking())
            .Select(l => new ManagementLeaseDetails
            {
                LeaseId = l.Id,
                LeaseRoleId = l.LeaseRoleId,
                ResidentId = l.ResidentId,
                UnitId = l.UnitId,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                IsActive = l.IsActive,
                Notes = l.Notes == null ? null : l.Notes.ToString()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (lease == null)
        {
            return new ManagementLeaseDetailsResult
            {
                LeaseNotFound = true,
                ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("LeaseWasNotFound") ?? "Lease was not found."
            };
        }

        return new ManagementLeaseDetailsResult
        {
            Success = true,
            Lease = lease
        };
    }

    private async Task<CreateValidationResult> ValidateCreateFromResidentAsync(
        ResidentDashboardContext context,
        ManagementLeaseCreateRequest request,
        CancellationToken cancellationToken)
    {
        var basicValidation = await ValidateSharedFieldsAsync(
            request.LeaseRoleId,
            request.StartDate,
            request.EndDate,
            request.ResidentId,
            request.UnitId,
            cancellationToken);

        if (!basicValidation.Success)
        {
            return new CreateValidationResult { Result = basicValidation };
        }

        if (request.ResidentId != context.ResidentId)
        {
            return new CreateValidationResult
            {
                Result = new ManagementLeaseCommandResult
                {
                    ResidentNotFound = true,
                    ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("ResidentWasNotFound") ?? "Resident was not found."
                }
            };
        }

        var unitMatch = await _dbContext.Units
            .AsNoTracking()
            .Where(u => u.Id == request.UnitId && u.Property!.Customer!.ManagementCompanyId == context.ManagementCompanyId)
            .Select(u => new { u.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (unitMatch == null)
        {
            return new CreateValidationResult
            {
                Result = new ManagementLeaseCommandResult
                {
                    UnitNotFound = true,
                    ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("UnitWasNotFound") ?? "Unit was not found."
                }
            };
        }

        var duplicateCheck = await HasOverlappingActiveLeaseAsync(context.ResidentId, unitMatch.Id, request.StartDate, request.EndDate, null, cancellationToken);
        if (duplicateCheck)
        {
            return new CreateValidationResult
            {
                Result = BuildDuplicateLeaseResult()
            };
        }

        return new CreateValidationResult
        {
            Success = true,
            UnitId = unitMatch.Id,
            Result = new ManagementLeaseCommandResult { Success = true }
        };
    }

    private async Task<CreateValidationResult> ValidateCreateFromUnitAsync(
        UnitDashboardContext context,
        ManagementLeaseCreateRequest request,
        CancellationToken cancellationToken)
    {
        var basicValidation = await ValidateSharedFieldsAsync(
            request.LeaseRoleId,
            request.StartDate,
            request.EndDate,
            request.ResidentId,
            request.UnitId,
            cancellationToken);

        if (!basicValidation.Success)
        {
            return new CreateValidationResult { Result = basicValidation };
        }

        if (request.UnitId != context.UnitId)
        {
            return new CreateValidationResult
            {
                Result = new ManagementLeaseCommandResult
                {
                    UnitNotFound = true,
                    ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("UnitWasNotFound") ?? "Unit was not found."
                }
            };
        }

        var residentMatch = await _dbContext.Residents
            .AsNoTracking()
            .Where(r => r.Id == request.ResidentId && r.ManagementCompanyId == context.ManagementCompanyId)
            .Select(r => new { r.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (residentMatch == null)
        {
            return new CreateValidationResult
            {
                Result = new ManagementLeaseCommandResult
                {
                    ResidentNotFound = true,
                    ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("ResidentWasNotFound") ?? "Resident was not found."
                }
            };
        }

        var duplicateCheck = await HasOverlappingActiveLeaseAsync(residentMatch.Id, context.UnitId, request.StartDate, request.EndDate, null, cancellationToken);
        if (duplicateCheck)
        {
            return new CreateValidationResult
            {
                Result = BuildDuplicateLeaseResult()
            };
        }

        return new CreateValidationResult
        {
            Success = true,
            ResidentId = residentMatch.Id,
            Result = new ManagementLeaseCommandResult { Success = true }
        };
    }

    private async Task<UpdateValidationResult> ValidateUpdateFromResidentAsync(
        ResidentDashboardContext context,
        ManagementLeaseUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var basicValidation = await ValidateSharedFieldsAsync(
            request.LeaseRoleId,
            request.StartDate,
            request.EndDate,
            context.ResidentId,
            Guid.Empty,
            cancellationToken,
            skipUnitValidation: true);

        if (!basicValidation.Success)
        {
            return new UpdateValidationResult { Result = basicValidation };
        }

        var lease = await _dbContext.Leases
            .AsTracking()
            .FirstOrDefaultAsync(
                l => l.Id == request.LeaseId && l.ResidentId == context.ResidentId && l.Resident!.ManagementCompanyId == context.ManagementCompanyId,
                cancellationToken);

        if (lease == null)
        {
            return new UpdateValidationResult
            {
                Result = new ManagementLeaseCommandResult
                {
                    LeaseNotFound = true,
                    ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("LeaseWasNotFound") ?? "Lease was not found."
                }
            };
        }

        var duplicateCheck = await HasOverlappingActiveLeaseAsync(lease.ResidentId, lease.UnitId, request.StartDate, request.EndDate, lease.Id, cancellationToken);
        if (duplicateCheck)
        {
            return new UpdateValidationResult
            {
                Result = BuildDuplicateLeaseResult()
            };
        }

        return new UpdateValidationResult
        {
            Success = true,
            Lease = lease,
            Result = new ManagementLeaseCommandResult { Success = true }
        };
    }

    private async Task<UpdateValidationResult> ValidateUpdateFromUnitAsync(
        UnitDashboardContext context,
        ManagementLeaseUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var basicValidation = await ValidateSharedFieldsAsync(
            request.LeaseRoleId,
            request.StartDate,
            request.EndDate,
            Guid.Empty,
            context.UnitId,
            cancellationToken,
            skipResidentValidation: true);

        if (!basicValidation.Success)
        {
            return new UpdateValidationResult { Result = basicValidation };
        }

        var lease = await _dbContext.Leases
            .AsTracking()
            .FirstOrDefaultAsync(
                l => l.Id == request.LeaseId && l.UnitId == context.UnitId && l.Unit!.PropertyId == context.PropertyId && l.Resident!.ManagementCompanyId == context.ManagementCompanyId,
                cancellationToken);

        if (lease == null)
        {
            return new UpdateValidationResult
            {
                Result = new ManagementLeaseCommandResult
                {
                    LeaseNotFound = true,
                    ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("LeaseWasNotFound") ?? "Lease was not found."
                }
            };
        }

        var duplicateCheck = await HasOverlappingActiveLeaseAsync(lease.ResidentId, lease.UnitId, request.StartDate, request.EndDate, lease.Id, cancellationToken);
        if (duplicateCheck)
        {
            return new UpdateValidationResult
            {
                Result = BuildDuplicateLeaseResult()
            };
        }

        return new UpdateValidationResult
        {
            Success = true,
            Lease = lease,
            Result = new ManagementLeaseCommandResult { Success = true }
        };
    }

    private async Task<ManagementLeaseCommandResult> ValidateSharedFieldsAsync(
        Guid leaseRoleId,
        DateOnly startDate,
        DateOnly? endDate,
        Guid residentId,
        Guid unitId,
        CancellationToken cancellationToken,
        bool skipResidentValidation = false,
        bool skipUnitValidation = false)
    {
        if (startDate == default)
        {
            return new ManagementLeaseCommandResult
            {
                InvalidStartDate = true,
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace(
                    "{0}",
                    App.Resources.Views.UiText.ResourceManager.GetString("StartDate", CultureInfo.CurrentUICulture) ?? "Start date")
            };
        }

        if (endDate.HasValue && endDate.Value < startDate)
        {
            return new ManagementLeaseCommandResult
            {
                InvalidEndDate = true,
                ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("InvalidDateRange") ?? "End date must be on or after start date."
            };
        }

        if (leaseRoleId == Guid.Empty)
        {
            return new ManagementLeaseCommandResult
            {
                InvalidLeaseRole = true,
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace(
                    "{0}",
                    App.Resources.Views.UiText.ResourceManager.GetString("LeaseRole") ?? "Lease role")
            };
        }

        var leaseRoleExists = await _dbContext.LeaseRoles
            .AsNoTracking()
            .AnyAsync(r => r.Id == leaseRoleId, cancellationToken);

        if (!leaseRoleExists)
        {
            return new ManagementLeaseCommandResult
            {
                InvalidLeaseRole = true,
                ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("InvalidData") ?? "Invalid data."
            };
        }

        if (!skipResidentValidation && residentId == Guid.Empty)
        {
            return new ManagementLeaseCommandResult
            {
                ResidentNotFound = true,
                ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("ResidentWasNotFound") ?? "Resident was not found."
            };
        }

        if (!skipUnitValidation && unitId == Guid.Empty)
        {
            return new ManagementLeaseCommandResult
            {
                UnitNotFound = true,
                ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("UnitWasNotFound") ?? "Unit was not found."
            };
        }

        return new ManagementLeaseCommandResult
        {
            Success = true
        };
    }

    private void ApplyUpdate(Lease lease, ManagementLeaseUpdateRequest request)
    {
        lease.LeaseRoleId = request.LeaseRoleId;
        lease.StartDate = request.StartDate;
        lease.EndDate = request.EndDate;
        lease.IsActive = request.IsActive;

        if (string.IsNullOrWhiteSpace(request.Notes))
        {
            lease.Notes = null;
            return;
        }

        var normalizedNotes = request.Notes.Trim();
        if (lease.Notes == null)
        {
            lease.Notes = new LangStr(normalizedNotes);
            return;
        }

        lease.Notes.SetTranslation(normalizedNotes);
        _dbContext.Entry(lease).Property(x => x.Notes).IsModified = true;
    }

    private async Task<bool> HasOverlappingActiveLeaseAsync(
        Guid residentId,
        Guid unitId,
        DateOnly startDate,
        DateOnly? endDate,
        Guid? excludedLeaseId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Leases
            .AsNoTracking()
            .Where(l => l.ResidentId == residentId && l.UnitId == unitId && l.IsActive)
            .Where(l => !excludedLeaseId.HasValue || l.Id != excludedLeaseId.Value)
            .AnyAsync(l => !l.EndDate.HasValue || l.EndDate.Value >= startDate)
            .ConfigureAwait(false);
    }

    private static ManagementLeaseCommandResult BuildDuplicateLeaseResult()
    {
        return new ManagementLeaseCommandResult
        {
            DuplicateActiveLease = true,
            ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("ActiveLeaseAlreadyExists")
                           ?? "An overlapping active lease already exists for the selected resident and unit."
        };
    }

    private sealed class CreateValidationResult
    {
        public bool Success { get; set; }
        public Guid? ResidentId { get; set; }
        public Guid? UnitId { get; set; }
        public ManagementLeaseCommandResult Result { get; set; } = new();
    }

    private sealed class UpdateValidationResult
    {
        public bool Success { get; set; }
        public Lease? Lease { get; set; }
        public ManagementLeaseCommandResult Result { get; set; } = new();
    }
}
