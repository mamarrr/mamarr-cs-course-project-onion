using App.BLL.Contracts.Units.Commands;
using App.BLL.Contracts.Units.Models;
using App.BLL.Contracts.Units.Queries;
using FluentResults;

namespace App.BLL.Contracts.Units.Services;

public interface IUnitProfileService
{
    Task<Result<UnitProfileModel>> GetAsync(
        GetUnitProfileQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<UnitProfileModel>> UpdateAsync(
        UpdateUnitCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        DeleteUnitCommand command,
        CancellationToken cancellationToken = default);
}
