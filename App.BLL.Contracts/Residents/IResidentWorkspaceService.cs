using App.BLL.DTO.Residents.Commands;
using App.BLL.DTO.Residents.Models;
using App.BLL.DTO.Residents.Queries;
using FluentResults;

namespace App.BLL.Contracts.Residents;

public interface IResidentWorkspaceService
{
    Task<Result<ResidentDashboardModel>> GetDashboardAsync(
        GetResidentProfileQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<CompanyResidentsModel>> GetResidentsAsync(
        GetResidentsQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<ResidentProfileModel>> CreateAsync(
        CreateResidentCommand command,
        CancellationToken cancellationToken = default);
}
