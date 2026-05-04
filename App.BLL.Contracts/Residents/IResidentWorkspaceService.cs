using App.BLL.Contracts.Residents.Commands;
using App.BLL.Contracts.Residents.Models;
using App.BLL.Contracts.Residents.Queries;
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
