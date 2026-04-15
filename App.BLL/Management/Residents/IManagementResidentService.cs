namespace App.BLL.Management;

public interface IManagementResidentService
{
    Task<ManagementResidentListResult> ListAsync(
        ManagementResidentsAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<ManagementResidentCreateResult> CreateAsync(
        ManagementResidentsAuthorizedContext context,
        ManagementResidentCreateRequest request,
        CancellationToken cancellationToken = default);
}
