namespace App.BLL.ResidentWorkspace.Residents;

public interface ICompanyResidentService
{
    Task<CompanyResidentListResult> ListAsync(
        CompanyResidentsAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<ResidentCreateResult> CreateAsync(
        CompanyResidentsAuthorizedContext context,
        ResidentCreateRequest request,
        CancellationToken cancellationToken = default);
}
