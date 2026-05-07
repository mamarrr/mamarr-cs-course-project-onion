using App.BLL.DTO.Admin.Dashboard;

namespace App.BLL.Contracts.Admin;

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
