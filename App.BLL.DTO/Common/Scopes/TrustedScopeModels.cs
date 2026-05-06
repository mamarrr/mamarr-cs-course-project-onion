using App.BLL.DTO.ManagementCompanies.Models;

namespace App.BLL.DTO.Common.Scopes;

public sealed record ManagementCompanyScope(
    Guid AppUserId,
    Guid ManagementCompanyId,
    string CompanySlug,
    CompanyMembershipContext Membership);

public sealed record CustomerScope(
    Guid AppUserId,
    Guid ManagementCompanyId,
    Guid CustomerId,
    string CompanySlug,
    string CustomerSlug,
    CompanyMembershipContext Membership);

public sealed record PropertyScope(
    Guid AppUserId,
    Guid ManagementCompanyId,
    Guid CustomerId,
    Guid PropertyId,
    string CompanySlug,
    string CustomerSlug,
    string PropertySlug,
    CompanyMembershipContext Membership);

public sealed record UnitScope(
    Guid AppUserId,
    Guid ManagementCompanyId,
    Guid CustomerId,
    Guid PropertyId,
    Guid UnitId,
    string CompanySlug,
    string CustomerSlug,
    string PropertySlug,
    string UnitSlug,
    CompanyMembershipContext Membership);

public sealed record ResidentScope(
    Guid AppUserId,
    Guid ManagementCompanyId,
    Guid ResidentId,
    string CompanySlug,
    string ResidentIdCode,
    CompanyMembershipContext? Membership);

public sealed record TicketScope(
    Guid AppUserId,
    Guid ManagementCompanyId,
    Guid TicketId,
    string CompanySlug,
    CompanyMembershipContext Membership,
    Guid? CustomerId = null,
    Guid? PropertyId = null,
    Guid? UnitId = null,
    Guid? ResidentId = null,
    Guid? VendorId = null);

public sealed record ResidentLeaseScope(
    Guid AppUserId,
    Guid ManagementCompanyId,
    Guid ResidentId,
    Guid LeaseId,
    string CompanySlug,
    string ResidentIdCode,
    CompanyMembershipContext Membership);

public sealed record UnitLeaseScope(
    Guid AppUserId,
    Guid ManagementCompanyId,
    Guid CustomerId,
    Guid PropertyId,
    Guid UnitId,
    Guid LeaseId,
    string CompanySlug,
    string CustomerSlug,
    string PropertySlug,
    string UnitSlug,
    CompanyMembershipContext Membership);
