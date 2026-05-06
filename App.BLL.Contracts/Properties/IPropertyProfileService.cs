using App.BLL.DTO.Properties.Commands;
using App.BLL.DTO.Properties.Models;
using App.BLL.DTO.Properties.Queries;
using FluentResults;

namespace App.BLL.Contracts.Properties;

public interface IPropertyProfileService
{
    Task<Result<PropertyProfileModel>> GetAsync(
        GetPropertyProfileQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<PropertyProfileModel>> UpdateAsync(
        UpdatePropertyProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        DeletePropertyCommand command,
        CancellationToken cancellationToken = default);
}
