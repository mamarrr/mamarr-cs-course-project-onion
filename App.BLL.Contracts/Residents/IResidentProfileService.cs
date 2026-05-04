using App.BLL.Contracts.Residents.Commands;
using App.BLL.Contracts.Residents.Models;
using App.BLL.Contracts.Residents.Queries;
using FluentResults;

namespace App.BLL.Contracts.Residents;

public interface IResidentProfileService
{
    Task<Result<ResidentProfileModel>> GetAsync(
        GetResidentProfileQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<ResidentProfileModel>> UpdateAsync(
        UpdateResidentProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        DeleteResidentCommand command,
        CancellationToken cancellationToken = default);
}
