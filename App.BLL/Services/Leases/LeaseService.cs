using System.Globalization;
using App.BLL.Contracts.Common;
using App.BLL.Contracts.Residents;
using App.BLL.Contracts.Units;
using App.BLL.Contracts.Leases;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Leases;
using App.BLL.DTO.Leases.Models;
using App.BLL.DTO.Residents.Models;
using App.BLL.DTO.Units.Models;
using App.BLL.Mappers.Leases;
using App.DAL.Contracts;
using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Leases;
using Base.BLL;
using FluentResults;

namespace App.BLL.Services.Leases;

public class LeaseService :
    BaseService<LeaseBllDto, LeaseDalDto, ILeaseRepository, IAppUOW>,
    ILeaseService
{
    private readonly IResidentService _residentService;
    private readonly IUnitService _unitService;

    public LeaseService(
        IAppUOW uow,
        IResidentService residentService,
        IUnitService unitService)
        : base(uow.Leases, uow, new LeaseBllDtoMapper())
    {
        _residentService = residentService;
        _unitService = unitService;
    }

    public async Task<Result<ResidentLeaseListModel>> ListForResidentAsync(
        ResidentRoute route,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveResidentAsync(route, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<ResidentLeaseListModel>(context.Errors);
        }

        var leases = await ServiceUOW.Leases.AllByResidentAsync(
            context.Value.ResidentId,
            context.Value.ManagementCompanyId,
            cancellationToken);

        return Result.Ok(new ResidentLeaseListModel
        {
            Leases = leases.Select(lease => new ResidentLeaseModel
            {
                LeaseId = lease.LeaseId,
                ResidentId = lease.ResidentId,
                UnitId = lease.UnitId,
                PropertyId = lease.PropertyId,
                PropertyName = lease.PropertyName,
                PropertySlug = lease.PropertySlug,
                UnitNr = lease.UnitNr,
                UnitSlug = lease.UnitSlug,
                LeaseRoleId = lease.LeaseRoleId,
                LeaseRoleCode = lease.LeaseRoleCode,
                LeaseRoleLabel = lease.LeaseRoleLabel,
                StartDate = lease.StartDate,
                EndDate = lease.EndDate,
                Notes = lease.Notes
            }).ToList()
        });
    }

    public async Task<Result<UnitLeaseListModel>> ListForUnitAsync(
        UnitRoute route,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveUnitAsync(route, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<UnitLeaseListModel>(context.Errors);
        }

        var leases = await ServiceUOW.Leases.AllByUnitAsync(
            context.Value.UnitId,
            context.Value.PropertyId,
            context.Value.ManagementCompanyId,
            cancellationToken);

        return Result.Ok(new UnitLeaseListModel
        {
            Leases = leases.Select(lease => new UnitLeaseModel
            {
                LeaseId = lease.LeaseId,
                ResidentId = lease.ResidentId,
                UnitId = lease.UnitId,
                PropertyId = lease.PropertyId,
                ResidentFullName = lease.ResidentFullName,
                ResidentIdCode = lease.ResidentIdCode,
                LeaseRoleId = lease.LeaseRoleId,
                LeaseRoleCode = lease.LeaseRoleCode,
                LeaseRoleLabel = lease.LeaseRoleLabel,
                StartDate = lease.StartDate,
                EndDate = lease.EndDate,
                Notes = lease.Notes
            }).ToList()
        });
    }

    public async Task<Result<LeaseModel>> GetForResidentAsync(
        ResidentLeaseRoute route,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveResidentAsync(route, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<LeaseModel>(context.Errors);
        }

        var lease = await ServiceUOW.Leases.FirstByIdForResidentAsync(
            route.LeaseId,
            context.Value.ResidentId,
            context.Value.ManagementCompanyId,
            cancellationToken);

        return lease is null
            ? Result.Fail(new NotFoundError(LeaseNotFoundMessage()))
            : Result.Ok(new LeaseModel
            {
                LeaseId = lease.LeaseId,
                LeaseRoleId = lease.LeaseRoleId,
                ResidentId = lease.ResidentId,
                UnitId = lease.UnitId,
                StartDate = lease.StartDate,
                EndDate = lease.EndDate,
                Notes = lease.Notes
            });
    }

    public async Task<Result<LeaseModel>> GetForUnitAsync(
        UnitLeaseRoute route,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveUnitAsync(route, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<LeaseModel>(context.Errors);
        }

        var lease = await ServiceUOW.Leases.FirstByIdForUnitAsync(
            route.LeaseId,
            context.Value.UnitId,
            context.Value.PropertyId,
            context.Value.ManagementCompanyId,
            cancellationToken);

        return lease is null
            ? Result.Fail(new NotFoundError(LeaseNotFoundMessage()))
            : Result.Ok(new LeaseModel
            {
                LeaseId = lease.LeaseId,
                LeaseRoleId = lease.LeaseRoleId,
                ResidentId = lease.ResidentId,
                UnitId = lease.UnitId,
                StartDate = lease.StartDate,
                EndDate = lease.EndDate,
                Notes = lease.Notes
            });
    }

    public async Task<Result<LeaseBllDto>> CreateForResidentAsync(
        ResidentRoute route,
        LeaseBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveResidentAsync(route, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<LeaseBllDto>(context.Errors);
        }

        var validation = await ValidateSharedFieldsAsync(
            dto.LeaseRoleId,
            dto.StartDate,
            dto.EndDate,
            cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<LeaseBllDto>(validation.Errors);
        }

        var unitExists = await ServiceUOW.Units.ExistsInCompanyAsync(
            dto.UnitId,
            context.Value.ManagementCompanyId,
            cancellationToken);
        if (!unitExists)
        {
            return Result.Fail<LeaseBllDto>(Validation(nameof(dto.UnitId), UnitNotFoundMessage()));
        }

        var hasOverlap = await ServiceUOW.Leases.HasOverlappingActiveLeaseAsync(
            context.Value.ResidentId,
            dto.UnitId,
            dto.StartDate,
            null,
            cancellationToken);
        if (hasOverlap)
        {
            return Result.Fail<LeaseBllDto>(new ConflictError(DuplicateLeaseMessage()));
        }

        dto.Id = Guid.Empty;
        dto.ResidentId = context.Value.ResidentId;

        return await AddAndFindCoreAsync(dto, default, cancellationToken);
    }

    public async Task<Result<LeaseBllDto>> CreateForUnitAsync(
        UnitRoute route,
        LeaseBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveUnitAsync(route, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<LeaseBllDto>(context.Errors);
        }

        var validation = await ValidateSharedFieldsAsync(
            dto.LeaseRoleId,
            dto.StartDate,
            dto.EndDate,
            cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<LeaseBllDto>(validation.Errors);
        }

        var residentExists = await ServiceUOW.Residents.ExistsInCompanyAsync(
            dto.ResidentId,
            context.Value.ManagementCompanyId,
            cancellationToken);
        if (!residentExists)
        {
            return Result.Fail<LeaseBllDto>(Validation(nameof(dto.ResidentId), ResidentNotFoundMessage()));
        }

        var hasOverlap = await ServiceUOW.Leases.HasOverlappingActiveLeaseAsync(
            dto.ResidentId,
            context.Value.UnitId,
            dto.StartDate,
            null,
            cancellationToken);
        if (hasOverlap)
        {
            return Result.Fail<LeaseBllDto>(new ConflictError(DuplicateLeaseMessage()));
        }

        dto.Id = Guid.Empty;
        dto.UnitId = context.Value.UnitId;

        return await AddAndFindCoreAsync(dto, default, cancellationToken);
    }

    public async Task<Result<LeaseModel>> CreateForResidentAndGetDetailsAsync(
        ResidentRoute route,
        LeaseBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var created = await CreateForResidentAsync(route, dto, cancellationToken);
        return created.IsFailed
            ? Result.Fail<LeaseModel>(created.Errors)
            : await GetForResidentAsync(ToResidentLeaseRoute(route, created.Value.Id), cancellationToken);
    }

    public async Task<Result<LeaseModel>> CreateForUnitAndGetDetailsAsync(
        UnitRoute route,
        LeaseBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var created = await CreateForUnitAsync(route, dto, cancellationToken);
        return created.IsFailed
            ? Result.Fail<LeaseModel>(created.Errors)
            : await GetForUnitAsync(ToUnitLeaseRoute(route, created.Value.Id), cancellationToken);
    }

    public async Task<Result<LeaseBllDto>> UpdateFromResidentAsync(
        ResidentLeaseRoute route,
        LeaseBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveResidentAsync(route, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<LeaseBllDto>(context.Errors);
        }

        var validation = await ValidateSharedFieldsAsync(
            dto.LeaseRoleId,
            dto.StartDate,
            dto.EndDate,
            cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<LeaseBllDto>(validation.Errors);
        }

        var lease = await ServiceUOW.Leases.FirstByIdForResidentAsync(
            route.LeaseId,
            context.Value.ResidentId,
            context.Value.ManagementCompanyId,
            cancellationToken);
        if (lease is null)
        {
            return Result.Fail<LeaseBllDto>(new NotFoundError(LeaseNotFoundMessage()));
        }

        var hasOverlap = await ServiceUOW.Leases.HasOverlappingActiveLeaseAsync(
            lease.ResidentId,
            lease.UnitId,
            dto.StartDate,
            lease.LeaseId,
            cancellationToken);
        if (hasOverlap)
        {
            return Result.Fail<LeaseBllDto>(new ConflictError(DuplicateLeaseMessage()));
        }

        dto.Id = route.LeaseId;
        dto.ResidentId = lease.ResidentId;
        dto.UnitId = lease.UnitId;

        var mapped = Mapper.Map(dto);
        if (mapped is null)
        {
            return Result.Fail<LeaseBllDto>("Entity mapping failed.");
        }

        var updated = await ServiceUOW.Leases.UpdateForResidentAsync(
            context.Value.ResidentId,
            context.Value.ManagementCompanyId,
            mapped,
            cancellationToken);
        if (!updated)
        {
            return Result.Fail<LeaseBllDto>(new NotFoundError(LeaseNotFoundMessage()));
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return await FindAsync(route.LeaseId, default, cancellationToken);
    }

    public async Task<Result<LeaseBllDto>> UpdateFromUnitAsync(
        UnitLeaseRoute route,
        LeaseBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveUnitAsync(route, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<LeaseBllDto>(context.Errors);
        }

        var validation = await ValidateSharedFieldsAsync(
            dto.LeaseRoleId,
            dto.StartDate,
            dto.EndDate,
            cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<LeaseBllDto>(validation.Errors);
        }

        var lease = await ServiceUOW.Leases.FirstByIdForUnitAsync(
            route.LeaseId,
            context.Value.UnitId,
            context.Value.PropertyId,
            context.Value.ManagementCompanyId,
            cancellationToken);
        if (lease is null)
        {
            return Result.Fail<LeaseBllDto>(new NotFoundError(LeaseNotFoundMessage()));
        }

        var hasOverlap = await ServiceUOW.Leases.HasOverlappingActiveLeaseAsync(
            lease.ResidentId,
            lease.UnitId,
            dto.StartDate,
            lease.LeaseId,
            cancellationToken);
        if (hasOverlap)
        {
            return Result.Fail<LeaseBllDto>(new ConflictError(DuplicateLeaseMessage()));
        }

        dto.Id = route.LeaseId;
        dto.ResidentId = lease.ResidentId;
        dto.UnitId = lease.UnitId;

        var mapped = Mapper.Map(dto);
        if (mapped is null)
        {
            return Result.Fail<LeaseBllDto>("Entity mapping failed.");
        }

        var updated = await ServiceUOW.Leases.UpdateForUnitAsync(
            context.Value.UnitId,
            context.Value.PropertyId,
            context.Value.ManagementCompanyId,
            mapped,
            cancellationToken);
        if (!updated)
        {
            return Result.Fail<LeaseBllDto>(new NotFoundError(LeaseNotFoundMessage()));
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return await FindAsync(route.LeaseId, default, cancellationToken);
    }

    public async Task<Result<LeaseModel>> UpdateFromResidentAndGetDetailsAsync(
        ResidentLeaseRoute route,
        LeaseBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var updated = await UpdateFromResidentAsync(route, dto, cancellationToken);
        return updated.IsFailed
            ? Result.Fail<LeaseModel>(updated.Errors)
            : await GetForResidentAsync(route, cancellationToken);
    }

    public async Task<Result<LeaseModel>> UpdateFromUnitAndGetDetailsAsync(
        UnitLeaseRoute route,
        LeaseBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var updated = await UpdateFromUnitAsync(route, dto, cancellationToken);
        return updated.IsFailed
            ? Result.Fail<LeaseModel>(updated.Errors)
            : await GetForUnitAsync(route, cancellationToken);
    }

    public async Task<Result> DeleteFromResidentAsync(
        ResidentLeaseRoute route,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveResidentAsync(route, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail(context.Errors);
        }

        var deleted = await ServiceUOW.Leases.DeleteForResidentAsync(
            route.LeaseId,
            context.Value.ResidentId,
            context.Value.ManagementCompanyId,
            cancellationToken);
        if (!deleted)
        {
            return Result.Fail(new NotFoundError(LeaseNotFoundMessage()));
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> DeleteFromUnitAsync(
        UnitLeaseRoute route,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveUnitAsync(route, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail(context.Errors);
        }

        var deleted = await ServiceUOW.Leases.DeleteForUnitAsync(
            route.LeaseId,
            context.Value.UnitId,
            context.Value.PropertyId,
            context.Value.ManagementCompanyId,
            cancellationToken);
        if (!deleted)
        {
            return Result.Fail(new NotFoundError(LeaseNotFoundMessage()));
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result<LeasePropertySearchResultModel>> SearchPropertiesAsync(
        ResidentRoute route,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveResidentAsync(route, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<LeasePropertySearchResultModel>(context.Errors);
        }

        var properties = await ServiceUOW.Properties.SearchForLeaseAssignmentAsync(
            context.Value.ManagementCompanyId,
            searchTerm,
            cancellationToken);

        return Result.Ok(new LeasePropertySearchResultModel
        {
            Properties = properties.Select(property => new LeasePropertySearchItemModel
            {
                PropertyId = property.PropertyId,
                CustomerId = property.CustomerId,
                PropertySlug = property.PropertySlug,
                PropertyName = property.PropertyName,
                CustomerSlug = property.CustomerSlug,
                CustomerName = property.CustomerName,
                AddressLine = property.AddressLine,
                City = property.City,
                PostalCode = property.PostalCode
            }).ToList()
        });
    }

    public async Task<Result<LeaseUnitOptionsModel>> ListUnitsForPropertyAsync(
        ResidentRoute route,
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveResidentAsync(route, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<LeaseUnitOptionsModel>(context.Errors);
        }

        var propertyExists = await ServiceUOW.Properties.ExistsInCompanyAsync(
            propertyId,
            context.Value.ManagementCompanyId,
            cancellationToken);
        if (!propertyExists)
        {
            return Result.Fail<LeaseUnitOptionsModel>(
                new NotFoundError(App.Resources.Views.UiText.ResourceManager.GetString("PropertyWasNotFound") ?? "Property was not found."));
        }

        var units = await ServiceUOW.Units.ListForLeaseAssignmentAsync(
            propertyId,
            context.Value.ManagementCompanyId,
            cancellationToken);

        return Result.Ok(new LeaseUnitOptionsModel
        {
            Units = units.Select(unit => new LeaseUnitOptionModel
            {
                UnitId = unit.UnitId,
                UnitSlug = unit.UnitSlug,
                UnitNr = unit.UnitNr,
                FloorNr = unit.FloorNr
            }).ToList()
        });
    }

    public async Task<Result<LeaseResidentSearchResultModel>> SearchResidentsAsync(
        UnitRoute route,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveUnitAsync(route, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<LeaseResidentSearchResultModel>(context.Errors);
        }

        var residents = await ServiceUOW.Residents.SearchForLeaseAssignmentAsync(
            context.Value.ManagementCompanyId,
            searchTerm,
            cancellationToken);

        return Result.Ok(new LeaseResidentSearchResultModel
        {
            Residents = residents.Select(resident => new LeaseResidentSearchItemModel
            {
                ResidentId = resident.ResidentId,
                FullName = resident.FullName,
                IdCode = resident.IdCode
            }).ToList()
        });
    }

    public async Task<Result<LeaseRoleOptionsModel>> ListLeaseRolesAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await ServiceUOW.Lookups.ListLeaseRolesAsync(cancellationToken);

        return Result.Ok(new LeaseRoleOptionsModel
        {
            Roles = roles.Select(role => new LeaseRoleOptionModel
            {
                LeaseRoleId = role.LeaseRoleId,
                Code = role.Code,
                Label = role.Label
            }).ToList()
        });
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

        var leaseRoleExists = await ServiceUOW.Lookups.LeaseRoleExistsAsync(leaseRoleId, cancellationToken);
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

    private Task<Result<ResidentWorkspaceModel>> ResolveResidentAsync(
        ResidentRoute route,
        CancellationToken cancellationToken)
    {
        return _residentService.ResolveWorkspaceAsync(route, cancellationToken);
    }

    private Task<Result<UnitWorkspaceModel>> ResolveUnitAsync(
        UnitRoute route,
        CancellationToken cancellationToken)
    {
        return _unitService.ResolveWorkspaceAsync(route, cancellationToken);
    }

    private static ResidentLeaseRoute ToResidentLeaseRoute(ResidentRoute route, Guid leaseId)
    {
        return new ResidentLeaseRoute
        {
            AppUserId = route.AppUserId,
            CompanySlug = route.CompanySlug,
            ResidentIdCode = route.ResidentIdCode,
            LeaseId = leaseId
        };
    }

    private static UnitLeaseRoute ToUnitLeaseRoute(UnitRoute route, Guid leaseId)
    {
        return new UnitLeaseRoute
        {
            AppUserId = route.AppUserId,
            CompanySlug = route.CompanySlug,
            CustomerSlug = route.CustomerSlug,
            PropertySlug = route.PropertySlug,
            UnitSlug = route.UnitSlug,
            LeaseId = leaseId
        };
    }

}
