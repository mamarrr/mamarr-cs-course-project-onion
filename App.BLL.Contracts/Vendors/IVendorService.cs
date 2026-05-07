using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Vendors;
using App.BLL.DTO.Vendors.Models;
using Base.BLL.Contracts;
using FluentResults;

namespace App.BLL.Contracts.Vendors;

public interface IVendorService : IBaseService<VendorBllDto>
{
    Task<Result<VendorWorkspaceModel>> ResolveCompanyWorkspaceAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<VendorListItemModel>>> ListForCompanyAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<VendorProfileModel>> GetProfileAsync(
        VendorRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<VendorBllDto>> CreateAsync(
        ManagementCompanyRoute route,
        VendorBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<VendorProfileModel>> CreateAndGetProfileAsync(
        ManagementCompanyRoute route,
        VendorBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<VendorBllDto>> UpdateAsync(
        VendorRoute route,
        VendorBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<VendorProfileModel>> UpdateAndGetProfileAsync(
        VendorRoute route,
        VendorBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        VendorRoute route,
        string confirmationRegistryCode,
        CancellationToken cancellationToken = default);
}
