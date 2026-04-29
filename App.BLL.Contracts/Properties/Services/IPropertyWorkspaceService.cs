using App.BLL.Contracts.Properties.Commands;
using App.BLL.Contracts.Properties.Models;
using App.BLL.Contracts.Properties.Queries;
using FluentResults;

namespace App.BLL.Contracts.Properties.Services;

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
