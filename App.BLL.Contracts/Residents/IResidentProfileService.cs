using App.BLL.DTO.Residents.Commands;
using App.BLL.DTO.Residents.Models;
using App.BLL.DTO.Residents.Queries;
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
