using App.BLL.DTO.Properties.Commands;
using App.BLL.DTO.Properties.Models;
using App.BLL.DTO.Properties.Queries;
using FluentResults;

namespace App.BLL.Contracts.Properties;

public interface IPropertyWorkspaceService
{
    Task<Result<PropertyWorkspaceModel>> GetWorkspaceAsync(
        GetPropertyWorkspaceQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<PropertyDashboardModel>> GetDashboardAsync(
        GetPropertyWorkspaceQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PropertyListItemModel>>> GetCustomerPropertiesAsync(
        GetPropertyWorkspaceQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<PropertyTypeOptionModel>>> GetPropertyTypeOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<Result<PropertyProfileModel>> CreateAsync(
        CreatePropertyCommand command,
        CancellationToken cancellationToken = default);
}
