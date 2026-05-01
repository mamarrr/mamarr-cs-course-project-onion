using System.Globalization;
using App.BLL.Contracts.Common;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Leases.Commands;
using App.BLL.Contracts.Leases.Models;
using App.BLL.Contracts.Leases.Queries;
using App.BLL.Contracts.Leases.Services;
using App.BLL.Mappers.Leases;
using App.Contracts;
using App.Contracts.DAL.Leases;
using FluentResults;

namespace App.BLL.Leases;

public class LeaseAssignmentService : ILeaseAssignmentService
{
    private readonly IAppUOW _uow;

    public LeaseAssignmentService(IAppUOW uow)
    {
        _uow = uow;
    }

    public async Task<Result<ResidentLeaseListModel>> ListForResidentAsync(
        GetResidentLeasesQuery query,
        CancellationToken cancellationToken = default)
    {
        var leases = await _uow.Leases.AllByResidentAsync(
            query.ResidentId,
            query.ManagementCompanyId,
            cancellationToken);

        return Result.Ok(new ResidentLeaseListModel
        {
            Leases = leases.Select(LeaseBllMapper.MapResidentLease).ToList()
        });
    }

    public async Task<Result<UnitLeaseListModel>> ListForUnitAsync(
        GetUnitLeasesQuery query,
        CancellationToken cancellationToken = default)
    {
        var leases = await _uow.Leases.AllByUnitAsync(
            query.UnitId,
            query.PropertyId,
            query.ManagementCompanyId,
            cancellationToken);

        return Result.Ok(new UnitLeaseListModel
        {
            Leases = leases.Select(LeaseBllMapper.MapUnitLease).ToList()
        });
    }

    public async Task<Result<LeaseModel>> GetForResidentAsync(
        GetResidentLeaseQuery query,
        CancellationToken cancellationToken = default)
    {
        var lease = await _uow.Leases.FirstByIdForResidentAsync(
            query.LeaseId,
            query.ResidentId,
            query.ManagementCompanyId,
            cancellationToken);

        return lease is null
            ? Result.Fail(new NotFoundError(LeaseNotFoundMessage()))
            : Result.Ok(LeaseBllMapper.MapLease(lease));
    }

    public async Task<Result<LeaseModel>> GetForUnitAsync(
        GetUnitLeaseQuery query,
        CancellationToken cancellationToken = default)
    {
        var lease = await _uow.Leases.FirstByIdForUnitAsync(
            query.LeaseId,
            query.UnitId,
            query.PropertyId,
            query.ManagementCompanyId,
            cancellationToken);

        return lease is null
            ? Result.Fail(new NotFoundError(LeaseNotFoundMessage()))
            : Result.Ok(LeaseBllMapper.MapLease(lease));
    }

    public async Task<Result<LeaseCommandModel>> CreateFromResidentAsync(
        CreateLeaseFromResidentCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateSharedFieldsAsync(
            command.LeaseRoleId,
            command.StartDate,
            command.EndDate,
            cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<LeaseCommandModel>(validation.Errors);
        }

        var unitExists = await _uow.Leases.UnitExistsInCompanyAsync(
            command.UnitId,
            command.ManagementCompanyId,
            cancellationToken);
        if (!unitExists)
        {
            return Result.Fail<LeaseCommandModel>(Validation(nameof(command.UnitId), UnitNotFoundMessage()));
        }

        var hasOverlap = await _uow.Leases.HasOverlappingActiveLeaseAsync(
            command.ResidentId,
            command.UnitId,
            command.StartDate,
            null,
            cancellationToken);
        if (hasOverlap)
        {
            return Result.Fail<LeaseCommandModel>(new ConflictError(DuplicateLeaseMessage()));
        }

        var lease = await _uow.Leases.AddAsync(
            new LeaseCreateDalDto
            {
                ResidentId = command.ResidentId,
                UnitId = command.UnitId,
                LeaseRoleId = command.LeaseRoleId,
                StartDate = command.StartDate,
                EndDate = command.EndDate,
                IsActive = command.IsActive,
                Notes = command.Notes
            },
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Ok(new LeaseCommandModel { LeaseId = lease.Id });
    }

    public async Task<Result<LeaseCommandModel>> CreateFromUnitAsync(
        CreateLeaseFromUnitCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateSharedFieldsAsync(
            command.LeaseRoleId,
            command.StartDate,
            command.EndDate,
            cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<LeaseCommandModel>(validation.Errors);
        }

        var residentExists = await _uow.Leases.ResidentExistsInCompanyAsync(
            command.ResidentId,
            command.ManagementCompanyId,
            cancellationToken);
        if (!residentExists)
        {
            return Result.Fail<LeaseCommandModel>(Validation(nameof(command.ResidentId), ResidentNotFoundMessage()));
        }

        var hasOverlap = await _uow.Leases.HasOverlappingActiveLeaseAsync(
            command.ResidentId,
            command.UnitId,
            command.StartDate,
            null,
            cancellationToken);
        if (hasOverlap)
        {
            return Result.Fail<LeaseCommandModel>(new ConflictError(DuplicateLeaseMessage()));
        }

        var lease = await _uow.Leases.AddAsync(
            new LeaseCreateDalDto
            {
                ResidentId = command.ResidentId,
                UnitId = command.UnitId,
                LeaseRoleId = command.LeaseRoleId,
                StartDate = command.StartDate,
                EndDate = command.EndDate,
                IsActive = command.IsActive,
                Notes = command.Notes
            },
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Ok(new LeaseCommandModel { LeaseId = lease.Id });
    }

    public async Task<Result<LeaseCommandModel>> UpdateFromResidentAsync(
        UpdateLeaseFromResidentCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateSharedFieldsAsync(
            command.LeaseRoleId,
            command.StartDate,
            command.EndDate,
            cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<LeaseCommandModel>(validation.Errors);
        }

        var lease = await _uow.Leases.FirstByIdForResidentAsync(
            command.LeaseId,
            command.ResidentId,
            command.ManagementCompanyId,
            cancellationToken);
        if (lease is null)
        {
            return Result.Fail<LeaseCommandModel>(new NotFoundError(LeaseNotFoundMessage()));
        }

        var hasOverlap = await _uow.Leases.HasOverlappingActiveLeaseAsync(
            lease.ResidentId,
            lease.UnitId,
            command.StartDate,
            lease.LeaseId,
            cancellationToken);
        if (hasOverlap)
        {
            return Result.Fail<LeaseCommandModel>(new ConflictError(DuplicateLeaseMessage()));
        }

        var updated = await _uow.Leases.UpdateForResidentAsync(
            command.ResidentId,
            command.ManagementCompanyId,
            LeaseBllMapper.ToUpdateDalDto(command),
            cancellationToken);
        if (!updated)
        {
            return Result.Fail<LeaseCommandModel>(new NotFoundError(LeaseNotFoundMessage()));
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Ok(new LeaseCommandModel { LeaseId = command.LeaseId });
    }

    public async Task<Result<LeaseCommandModel>> UpdateFromUnitAsync(
        UpdateLeaseFromUnitCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateSharedFieldsAsync(
            command.LeaseRoleId,
            command.StartDate,
            command.EndDate,
            cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<LeaseCommandModel>(validation.Errors);
        }

        var lease = await _uow.Leases.FirstByIdForUnitAsync(
            command.LeaseId,
            command.UnitId,
            command.PropertyId,
            command.ManagementCompanyId,
            cancellationToken);
        if (lease is null)
        {
            return Result.Fail<LeaseCommandModel>(new NotFoundError(LeaseNotFoundMessage()));
        }

        var hasOverlap = await _uow.Leases.HasOverlappingActiveLeaseAsync(
            lease.ResidentId,
            lease.UnitId,
            command.StartDate,
            lease.LeaseId,
            cancellationToken);
        if (hasOverlap)
        {
            return Result.Fail<LeaseCommandModel>(new ConflictError(DuplicateLeaseMessage()));
        }

        var updated = await _uow.Leases.UpdateForUnitAsync(
            command.UnitId,
            command.PropertyId,
            command.ManagementCompanyId,
            LeaseBllMapper.ToUpdateDalDto(command),
            cancellationToken);
        if (!updated)
        {
            return Result.Fail<LeaseCommandModel>(new NotFoundError(LeaseNotFoundMessage()));
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Ok(new LeaseCommandModel { LeaseId = command.LeaseId });
    }

    public async Task<Result> DeleteFromResidentAsync(
        DeleteLeaseFromResidentCommand command,
        CancellationToken cancellationToken = default)
    {
        var deleted = await _uow.Leases.DeleteForResidentAsync(
            command.LeaseId,
            command.ResidentId,
            command.ManagementCompanyId,
            cancellationToken);
        if (!deleted)
        {
            return Result.Fail(new NotFoundError(LeaseNotFoundMessage()));
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> DeleteFromUnitAsync(
        DeleteLeaseFromUnitCommand command,
        CancellationToken cancellationToken = default)
    {
        var deleted = await _uow.Leases.DeleteForUnitAsync(
            command.LeaseId,
            command.UnitId,
            command.PropertyId,
            command.ManagementCompanyId,
            cancellationToken);
        if (!deleted)
        {
            return Result.Fail(new NotFoundError(LeaseNotFoundMessage()));
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    private async Task<Result> ValidateSharedFieldsAsync(
        Guid leaseRoleId,
        DateOnly startDate,
        DateOnly? endDate,
        CancellationToken cancellationToken)
    {
        if (startDate == default)
        {
            return Result.Fail(Validation("StartDate", App.Resources.Views.UiText.RequiredField.Replace(
                "{0}",
                App.Resources.Views.UiText.ResourceManager.GetString("StartDate", CultureInfo.CurrentUICulture) ?? "Start date")));
        }

        if (endDate.HasValue && endDate.Value < startDate)
        {
            return Result.Fail(Validation("EndDate", App.Resources.Views.UiText.ResourceManager.GetString("InvalidDateRange") ?? "End date must be on or after start date."));
        }

        if (leaseRoleId == Guid.Empty)
        {
            return Result.Fail(Validation("LeaseRoleId", App.Resources.Views.UiText.RequiredField.Replace(
                "{0}",
                App.Resources.Views.UiText.ResourceManager.GetString("LeaseRole") ?? "Lease role")));
        }

        var leaseRoleExists = await _uow.Leases.LeaseRoleExistsAsync(leaseRoleId, cancellationToken);
        if (!leaseRoleExists)
        {
            return Result.Fail(Validation("LeaseRoleId", App.Resources.Views.UiText.ResourceManager.GetString("InvalidData") ?? "Invalid data."));
        }

        return Result.Ok();
    }

    private static ValidationAppError Validation(string propertyName, string message)
    {
        return new ValidationAppError(
            message,
            new[]
            {
                new ValidationFailureModel
                {
                    PropertyName = propertyName,
                    ErrorMessage = message
                }
            });
    }

    private static string LeaseNotFoundMessage()
        => App.Resources.Views.UiText.ResourceManager.GetString("LeaseWasNotFound") ?? "Lease was not found.";

    private static string ResidentNotFoundMessage()
        => App.Resources.Views.UiText.ResourceManager.GetString("ResidentWasNotFound") ?? "Resident was not found.";

    private static string UnitNotFoundMessage()
        => App.Resources.Views.UiText.ResourceManager.GetString("UnitWasNotFound") ?? "Unit was not found.";

    private static string DuplicateLeaseMessage()
        => App.Resources.Views.UiText.ResourceManager.GetString("ActiveLeaseAlreadyExists")
           ?? "An overlapping active lease already exists for the selected resident and unit.";
}
