using Base.DAL.Contracts;
using App.Contracts.DAL.Lookups;

namespace App.Contracts.DAL.ManagementCompanies;

public interface IManagementCompanyRepository : IBaseRepository<ManagementCompanyDalDto>
{
    Task<ManagementCompanyDalDto?> FirstBySlugAsync(
        string companySlug,
        CancellationToken cancellationToken = default);

    Task<string?> FindActiveUserRoleCodeAsync(
        Guid appUserId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<ManagementCompanyProfileDalDto?> FirstProfileBySlugAsync(
        string companySlug,
        CancellationToken cancellationToken = default);

    Task<ManagementCompanyProfileDalDto?> FirstProfileByIdAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<ManagementCompanyDalDto?> FirstActiveByRegistryCodeAsync(
        string registryCode,
        CancellationToken cancellationToken = default);

    Task<bool> RegistryCodeExistsAsync(
        string registryCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> AllSlugsAsync(
        CancellationToken cancellationToken = default);

    Task<ManagementCompanyDalDto> AddManagementCompanyAsync(
        ManagementCompanyCreateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ManagementCompanyContextDalDto>> ActiveUserManagementContextsAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);

    Task<ManagementCompanyContextDalDto?> ActiveUserManagementContextByCompanyIdAsync(
        Guid appUserId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> ActiveUserManagementContextExistsBySlugAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);

    Task<ManagementCompanyMembershipDalDto?> FirstMembershipByUserAndCompanyAsync(
        Guid appUserId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ManagementCompanyMembershipDalDto>> MembersByCompanyAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<ManagementCompanyMembershipDalDto?> FindMemberByIdAndCompanyAsync(
        Guid membershipId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ManagementCompanyMembershipDalDto>> FindMembersByIdsAndCompanyAsync(
        Guid managementCompanyId,
        IReadOnlyCollection<Guid> membershipIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LookupDalDto>> AllManagementCompanyRolesAsync(
        CancellationToken cancellationToken = default);

    Task<LookupDalDto?> FindManagementCompanyRoleByIdAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task<Guid?> FindAppUserIdByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    Task<bool> UserBelongsToCompanyAsync(
        Guid appUserId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> MembershipExistsAsync(
        Guid appUserId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> RegistryCodeExistsOutsideCompanyAsync(
        Guid managementCompanyId,
        string normalizedRegistryCode,
        CancellationToken cancellationToken = default);

    void AddMembership(ManagementCompanyMembershipCreateDalDto dto);

    Task<bool> ApplyMembershipUpdateAsync(
        ManagementCompanyMembershipUpdateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveMembershipAsync(
        Guid membershipId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateProfileAsync(
        ManagementCompanyProfileUpdateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> SetMembershipRoleAsync(
        Guid membershipId,
        Guid managementCompanyId,
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task<int> CountEffectiveOwnersAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteCascadeAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);
}
