using App.DAL.DTO.Admin.Dashboard;

namespace App.DAL.Contracts.Repositories.Admin;

public interface IAdminDashboardRepository
{
    Task<AdminDashboardDalDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
