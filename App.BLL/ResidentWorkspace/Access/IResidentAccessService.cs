using App.BLL.ResidentWorkspace.Residents;

namespace App.BLL.ResidentWorkspace.Access;

public interface IResidentAccessService
{
    Task<CompanyResidentsAuthorizationResult> AuthorizeAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);

    Task<ResidentDashboardAccessResult> ResolveDashboardAccessAsync(
        Guid appUserId,
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken = default);
}
