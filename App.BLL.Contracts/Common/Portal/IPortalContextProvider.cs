using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Customers.Models;
using App.BLL.DTO.ManagementCompanies.Models;
using App.BLL.DTO.Properties.Models;
using App.BLL.DTO.Residents.Models;
using App.BLL.DTO.Units.Models;
using FluentResults;

namespace App.BLL.Contracts.Common.Portal;

public interface IPortalContextProvider
{
    Task<Result<CompanyMembershipContext>> AuthorizeManagementAreaAccessAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<CompanyWorkspaceModel>> ResolveCompanyWorkspaceAsync(
        ManagementCompanyRoute route,
        IReadOnlySet<string> allowedRoleCodes,
        CancellationToken cancellationToken = default);

    Task<Result<CustomerWorkspaceModel>> ResolveCustomerWorkspaceAsync(
        CustomerRoute route,
        IReadOnlySet<string> managementRoleCodes,
        bool allowCustomerContext,
        CancellationToken cancellationToken = default);

    Task<Result<PropertyWorkspaceModel>> ResolvePropertyWorkspaceAsync(
        PropertyRoute route,
        IReadOnlySet<string> managementRoleCodes,
        bool allowCustomerContext,
        CancellationToken cancellationToken = default);

    Task<Result<UnitWorkspaceModel>> ResolveUnitWorkspaceAsync(
        UnitRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<CompanyResidentsModel>> ResolveCompanyResidentsContextAsync(
        ManagementCompanyRoute route,
        IReadOnlySet<string> allowedRoleCodes,
        CancellationToken cancellationToken = default);

    Task<Result<ResidentWorkspaceModel>> ResolveResidentWorkspaceAsync(
        ResidentRoute route,
        IReadOnlySet<string> managementRoleCodes,
        bool allowResidentContext,
        CancellationToken cancellationToken = default);
}
