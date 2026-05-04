using App.BLL.Contracts.Residents.Models;
using App.BLL.Contracts.Residents.Queries;
using FluentResults;

namespace App.BLL.Contracts.Residents;

public interface IResidentAccessService
{
    Task<Result<CompanyResidentsModel>> ResolveCompanyResidentsAsync(
        GetResidentsQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<ResidentWorkspaceModel>> ResolveResidentWorkspaceAsync(
        GetResidentProfileQuery query,
        CancellationToken cancellationToken = default);
}
