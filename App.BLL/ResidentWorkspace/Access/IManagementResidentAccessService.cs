using App.BLL.ResidentWorkspace.Residents;

namespace App.BLL.ResidentWorkspace.Access;

public interface IManagementResidentAccessService
{
    Task<ManagementResidentsAuthorizationResult> AuthorizeAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);

    Task<ManagementResidentDashboardAccessResult> ResolveDashboardAccessAsync(
        Guid appUserId,
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken = default);
}
