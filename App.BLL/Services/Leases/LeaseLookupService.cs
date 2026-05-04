using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Leases.Models;
using App.BLL.Contracts.Leases.Queries;
using App.BLL.Contracts.Leases.Services;
using App.BLL.Mappers.Leases;
using App.DAL.Contracts;
using FluentResults;

namespace App.BLL.Leases;

public class LeaseLookupService : ILeaseLookupService
{
    private readonly IAppUOW _uow;

    public LeaseLookupService(IAppUOW uow)
    {
        _uow = uow;
    }

    public async Task<Result<LeasePropertySearchResultModel>> SearchPropertiesAsync(
        SearchLeasePropertiesQuery query,
        CancellationToken cancellationToken = default)
    {
        var properties = await _uow.Leases.SearchPropertiesAsync(
            query.ManagementCompanyId,
            query.SearchTerm,
            cancellationToken);

        return Result.Ok(new LeasePropertySearchResultModel
        {
            Properties = properties.Select(LeaseBllMapper.MapProperty).ToList()
        });
    }

    public async Task<Result<LeaseUnitOptionsModel>> ListUnitsForPropertyAsync(
        GetLeaseUnitsForPropertyQuery query,
        CancellationToken cancellationToken = default)
    {
        var propertyExists = await _uow.Leases.PropertyExistsInCompanyAsync(
            query.PropertyId,
            query.ManagementCompanyId,
            cancellationToken);
        if (!propertyExists)
        {
            return Result.Fail<LeaseUnitOptionsModel>(
                new NotFoundError(App.Resources.Views.UiText.ResourceManager.GetString("PropertyWasNotFound") ?? "Property was not found."));
        }

        var units = await _uow.Leases.ListUnitsForPropertyAsync(
            query.PropertyId,
            query.ManagementCompanyId,
            cancellationToken);

        return Result.Ok(new LeaseUnitOptionsModel
        {
            Units = units.Select(LeaseBllMapper.MapUnitOption).ToList()
        });
    }

    public async Task<Result<LeaseResidentSearchResultModel>> SearchResidentsAsync(
        SearchLeaseResidentsQuery query,
        CancellationToken cancellationToken = default)
    {
        var residents = await _uow.Leases.SearchResidentsAsync(
            query.ManagementCompanyId,
            query.SearchTerm,
            cancellationToken);

        return Result.Ok(new LeaseResidentSearchResultModel
        {
            Residents = residents.Select(LeaseBllMapper.MapResidentSearchItem).ToList()
        });
    }

    public async Task<Result<LeaseRoleOptionsModel>> ListLeaseRolesAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await _uow.Leases.ListLeaseRolesAsync(cancellationToken);

        return Result.Ok(new LeaseRoleOptionsModel
        {
            Roles = roles.Select(LeaseBllMapper.MapLeaseRole).ToList()
        });
    }
}
