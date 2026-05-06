using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Properties;
using App.BLL.DTO.Properties.Models;
using Base.BLL.Contracts;
using FluentResults;

namespace App.BLL.Contracts.Properties;

public interface IPropertyService : IBaseService<PropertyBllDto>
{
    Task<Result<PropertyWorkspaceModel>> GetWorkspaceAsync(
        PropertyRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<PropertyDashboardModel>> GetDashboardAsync(
        PropertyRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PropertyListItemModel>>> ListForCustomerAsync(
        CustomerRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<PropertyProfileModel>> GetProfileAsync(
        PropertyRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PropertyTypeOptionModel>>> GetPropertyTypeOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<Result<PropertyBllDto>> CreateAsync(
        CustomerRoute route,
        PropertyBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<PropertyProfileModel>> CreateAndGetProfileAsync(
        CustomerRoute route,
        PropertyBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<PropertyBllDto>> UpdateAsync(
        PropertyRoute route,
        PropertyBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<PropertyProfileModel>> UpdateAndGetProfileAsync(
        PropertyRoute route,
        PropertyBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        PropertyRoute route,
        string confirmationName,
        CancellationToken cancellationToken = default);
}
