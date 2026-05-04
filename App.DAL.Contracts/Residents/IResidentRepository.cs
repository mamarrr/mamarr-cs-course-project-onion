using Base.DAL.Contracts;

namespace App.DAL.Contracts.DAL.Residents;

public interface IResidentRepository : IBaseRepository<ResidentDalDto>
{
    Task<ResidentProfileDalDto?> FirstProfileAsync(
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken = default);

    Task<ResidentProfileDalDto?> FindProfileAsync(
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResidentListItemDalDto>> AllByCompanyAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<ResidentUserContextDalDto?> FirstActiveUserResidentContextAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveUserResidentContextAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);

    Task<bool> IdCodeExistsForCompanyAsync(
        Guid managementCompanyId,
        string idCode,
        Guid? exceptResidentId = null,
        CancellationToken cancellationToken = default);

    Task<ResidentDalDto> AddAsync(
        ResidentCreateDalDto dto,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ResidentUpdateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResidentContactDalDto>> ContactsByResidentAsync(
        Guid residentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResidentLeaseSummaryDalDto>> LeaseSummariesByResidentAsync(
        Guid residentId,
        CancellationToken cancellationToken = default);
}
