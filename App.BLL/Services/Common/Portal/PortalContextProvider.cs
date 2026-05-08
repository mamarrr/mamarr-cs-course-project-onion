using App.BLL.Contracts.Common.Portal;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Customers.Models;
using App.BLL.DTO.ManagementCompanies.Models;
using App.BLL.DTO.Properties.Models;
using App.BLL.DTO.Residents.Models;
using App.BLL.DTO.Units.Models;
using App.DAL.Contracts;
using App.DAL.DTO.Residents;
using FluentResults;

namespace App.BLL.Services.Common.Portal;

public class PortalContextProvider : IPortalContextProvider
{
    private const string OwnerRoleCode = "OWNER";
    private const string ManagerRoleCode = "MANAGER";

    private static readonly HashSet<string> AdminRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        OwnerRoleCode,
        ManagerRoleCode
    };

    private static readonly HashSet<string> ManagementAreaRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        OwnerRoleCode,
        ManagerRoleCode,
        "FINANCE",
        "SUPPORT"
    };

    private readonly IAppUOW _uow;

    public PortalContextProvider(IAppUOW uow)
    {
        _uow = uow;
    }

    public async Task<Result<CompanyMembershipContext>> AuthorizeManagementAreaAccessAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default)
    {
        var resolution = await ResolveMembershipContextAsync(route, cancellationToken);
        if (resolution.IsFailed)
        {
            return Result.Fail(resolution.Errors);
        }

        if (!ManagementAreaRoleCodes.Contains(resolution.Value.ActorRoleCode))
        {
            return Result.Fail<CompanyMembershipContext>(WithAuthorizationReason(
                new ForbiddenError("You do not have access to the management area."),
                CompanyMembershipAuthorizationFailureReason.InsufficientPrivileges));
        }

        return Result.Ok(resolution.Value);
    }

    public async Task<Result<CompanyWorkspaceModel>> ResolveCompanyWorkspaceAsync(
        ManagementCompanyRoute route,
        IReadOnlySet<string> allowedRoleCodes,
        CancellationToken cancellationToken = default)
    {
        if (route.AppUserId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authentication is required."));
        }

        if (string.IsNullOrWhiteSpace(route.CompanySlug))
        {
            return Result.Fail(new NotFoundError(App.Resources.Views.UiText.ManagementCompanyWasNotFound));
        }

        var company = await _uow.ManagementCompanies.FirstBySlugAsync(route.CompanySlug, cancellationToken);
        if (company is null)
        {
            return Result.Fail(new NotFoundError(App.Resources.Views.UiText.ManagementCompanyWasNotFound));
        }

        var roleCode = await _uow.ManagementCompanies.FindActiveUserRoleCodeAsync(
            route.AppUserId,
            company.Id,
            cancellationToken);

        if (roleCode is null || !allowedRoleCodes.Contains(roleCode))
        {
            return Result.Fail(new ForbiddenError(App.Resources.Views.UiText.AccessDeniedDescription));
        }

        return Result.Ok(new CompanyWorkspaceModel
        {
            AppUserId = route.AppUserId,
            ManagementCompanyId = company.Id,
            CompanySlug = company.Slug,
            CompanyName = company.Name,
            RoleCode = roleCode
        });
    }

    public async Task<Result<CustomerWorkspaceModel>> ResolveCustomerWorkspaceAsync(
        CustomerRoute route,
        IReadOnlySet<string> managementRoleCodes,
        bool allowCustomerContext,
        CancellationToken cancellationToken = default)
    {
        if (route.AppUserId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authentication is required."));
        }

        var company = await _uow.ManagementCompanies.FirstBySlugAsync(route.CompanySlug, cancellationToken);
        if (company is null)
        {
            return Result.Fail(new NotFoundError(App.Resources.Views.UiText.ManagementCompanyWasNotFound));
        }

        if (string.IsNullOrWhiteSpace(route.CustomerSlug))
        {
            return Result.Fail(new NotFoundError("Customer context was not found."));
        }

        var customer = await _uow.Customers.FirstWorkspaceByCompanyAndSlugAsync(
            company.Id,
            route.CustomerSlug,
            cancellationToken);

        if (customer is null)
        {
            return Result.Fail(new NotFoundError("Customer context was not found."));
        }

        var roleCode = await _uow.ManagementCompanies.FindActiveUserRoleCodeAsync(
            route.AppUserId,
            company.Id,
            cancellationToken);
        if (roleCode is not null && managementRoleCodes.Contains(roleCode))
        {
            return Result.Ok(ToCustomerWorkspace(route.AppUserId, customer));
        }

        if (!allowCustomerContext)
        {
            return Result.Fail(new ForbiddenError(App.Resources.Views.UiText.AccessDeniedDescription));
        }

        var hasCustomerContext = await _uow.Customers.ActiveUserCustomerContextExistsAsync(
            route.AppUserId,
            customer.Id,
            cancellationToken);

        return hasCustomerContext
            ? Result.Ok(ToCustomerWorkspace(route.AppUserId, customer))
            : Result.Fail(new ForbiddenError(App.Resources.Views.UiText.AccessDeniedDescription));
    }

    public async Task<Result<PropertyWorkspaceModel>> ResolvePropertyWorkspaceAsync(
        PropertyRoute route,
        IReadOnlySet<string> managementRoleCodes,
        bool allowCustomerContext,
        CancellationToken cancellationToken = default)
    {
        var customer = await ResolveCustomerWorkspaceAsync(
            route,
            managementRoleCodes,
            allowCustomerContext,
            cancellationToken);
        if (customer.IsFailed)
        {
            return Result.Fail(customer.Errors);
        }

        var property = await _uow.Properties.FirstWorkspaceByCustomerAndSlugAsync(
            customer.Value.CustomerId,
            route.PropertySlug,
            cancellationToken);

        return property is null
            ? Result.Fail(new NotFoundError("Property context was not found."))
            : Result.Ok(new PropertyWorkspaceModel
            {
                AppUserId = customer.Value.AppUserId,
                ManagementCompanyId = customer.Value.ManagementCompanyId,
                CompanySlug = customer.Value.CompanySlug,
                CompanyName = customer.Value.CompanyName,
                CustomerId = customer.Value.CustomerId,
                CustomerSlug = customer.Value.CustomerSlug,
                CustomerName = customer.Value.CustomerName,
                PropertyId = property.Id,
                PropertySlug = property.Slug,
                PropertyName = property.Name
            });
    }

    public async Task<Result<UnitWorkspaceModel>> ResolveUnitWorkspaceAsync(
        UnitRoute route,
        CancellationToken cancellationToken = default)
    {
        if (route.AppUserId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authentication is required."));
        }

        if (string.IsNullOrWhiteSpace(route.CompanySlug)
            || string.IsNullOrWhiteSpace(route.CustomerSlug)
            || string.IsNullOrWhiteSpace(route.PropertySlug)
            || string.IsNullOrWhiteSpace(route.UnitSlug))
        {
            return Result.Fail(new NotFoundError("Unit context was not found."));
        }

        var company = await _uow.ManagementCompanies.FirstBySlugAsync(
            route.CompanySlug,
            cancellationToken);
        if (company is null)
        {
            return Result.Fail(new NotFoundError("Unit context was not found."));
        }

        var roleCode = await _uow.Customers.FindActiveManagementCompanyRoleCodeAsync(
            company.Id,
            route.AppUserId,
            cancellationToken);
        if (roleCode is null)
        {
            return Result.Fail(new ForbiddenError(App.Resources.Views.UiText.AccessDeniedDescription));
        }

        var unit = await _uow.Units.FirstDashboardAsync(
            route.CompanySlug,
            route.CustomerSlug,
            route.PropertySlug,
            route.UnitSlug,
            cancellationToken);

        return unit is null
            ? Result.Fail(new NotFoundError("Unit context was not found."))
            : Result.Ok(new UnitWorkspaceModel
            {
                AppUserId = route.AppUserId,
                ManagementCompanyId = unit.ManagementCompanyId,
                CompanySlug = unit.CompanySlug,
                CompanyName = unit.CompanyName,
                CustomerId = unit.CustomerId,
                CustomerSlug = unit.CustomerSlug,
                CustomerName = unit.CustomerName,
                PropertyId = unit.PropertyId,
                PropertySlug = unit.PropertySlug,
                PropertyName = unit.PropertyName,
                UnitId = unit.Id,
                UnitSlug = unit.Slug,
                UnitNr = unit.UnitNr
            });
    }

    public async Task<Result<CompanyResidentsModel>> ResolveCompanyResidentsContextAsync(
        ManagementCompanyRoute route,
        IReadOnlySet<string> allowedRoleCodes,
        CancellationToken cancellationToken = default)
    {
        if (route.AppUserId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authentication is required."));
        }

        if (string.IsNullOrWhiteSpace(route.CompanySlug))
        {
            return Result.Fail(new NotFoundError(App.Resources.Views.UiText.ManagementCompanyWasNotFound));
        }

        var company = await _uow.ManagementCompanies.FirstBySlugAsync(
            route.CompanySlug,
            cancellationToken);
        if (company is null)
        {
            return Result.Fail(new NotFoundError(App.Resources.Views.UiText.ManagementCompanyWasNotFound));
        }

        var roleCode = await _uow.ManagementCompanies.FindActiveUserRoleCodeAsync(
            route.AppUserId,
            company.Id,
            cancellationToken);

        if (roleCode is null || !allowedRoleCodes.Contains(roleCode))
        {
            return Result.Fail(new ForbiddenError("Access denied."));
        }

        return Result.Ok(new CompanyResidentsModel
        {
            AppUserId = route.AppUserId,
            ManagementCompanyId = company.Id,
            CompanySlug = company.Slug,
            CompanyName = company.Name
        });
    }

    public async Task<Result<ResidentWorkspaceModel>> ResolveResidentWorkspaceAsync(
        ResidentRoute route,
        IReadOnlySet<string> managementRoleCodes,
        bool allowResidentContext,
        CancellationToken cancellationToken = default)
    {
        if (route.AppUserId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authentication is required."));
        }

        if (string.IsNullOrWhiteSpace(route.CompanySlug))
        {
            return Result.Fail(new NotFoundError("Resident context was not found."));
        }

        var company = await _uow.ManagementCompanies.FirstBySlugAsync(
            route.CompanySlug,
            cancellationToken);
        if (company is null)
        {
            return Result.Fail(new NotFoundError("Resident context was not found."));
        }

        if (string.IsNullOrWhiteSpace(route.ResidentIdCode))
        {
            return Result.Fail(new NotFoundError("Resident context was not found."));
        }

        var resident = await _uow.Residents.FirstProfileAsync(
            route.CompanySlug,
            route.ResidentIdCode,
            cancellationToken);

        if (resident is null || resident.ManagementCompanyId != company.Id)
        {
            return Result.Fail(new NotFoundError("Resident context was not found."));
        }

        var roleCode = await _uow.ManagementCompanies.FindActiveUserRoleCodeAsync(
            route.AppUserId,
            company.Id,
            cancellationToken);
        if (roleCode is not null && managementRoleCodes.Contains(roleCode))
        {
            return Result.Ok(ToResidentWorkspace(route.AppUserId, resident));
        }

        if (!allowResidentContext)
        {
            return Result.Fail(new ForbiddenError("Access denied."));
        }

        var hasResidentContext = await _uow.Residents.HasActiveUserResidentContextAsync(
            route.AppUserId,
            resident.Id,
            cancellationToken);

        return hasResidentContext
            ? Result.Ok(ToResidentWorkspace(route.AppUserId, resident))
            : Result.Fail(new ForbiddenError("Access denied."));
    }

    private async Task<Result<CompanyMembershipContext>> ResolveMembershipContextAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = route.CompanySlug.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return Result.Fail<CompanyMembershipContext>(WithAuthorizationReason(
                new NotFoundError("Company slug is required."),
                CompanyMembershipAuthorizationFailureReason.CompanyNotFound));
        }

        var company = await _uow.ManagementCompanies.FirstBySlugAsync(normalizedSlug, cancellationToken);
        if (company is null)
        {
            return Result.Fail<CompanyMembershipContext>(WithAuthorizationReason(
                new NotFoundError("Company not found."),
                CompanyMembershipAuthorizationFailureReason.CompanyNotFound));
        }

        var actorMembership = await _uow.ManagementCompanies.FirstMembershipByUserAndCompanyAsync(
            route.AppUserId,
            company.Id,
            cancellationToken);

        if (actorMembership is null)
        {
            return Result.Fail<CompanyMembershipContext>(WithAuthorizationReason(
                new ForbiddenError("You do not have access to this company."),
                CompanyMembershipAuthorizationFailureReason.MembershipNotFound));
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!IsMembershipEffective(actorMembership.ValidFrom, actorMembership.ValidTo, today))
        {
            return Result.Fail<CompanyMembershipContext>(WithAuthorizationReason(
                new ForbiddenError("Your company membership is not currently effective."),
                CompanyMembershipAuthorizationFailureReason.MembershipNotEffective));
        }

        return Result.Ok(new CompanyMembershipContext
        {
            AppUserId = route.AppUserId,
            ManagementCompanyId = company.Id,
            CompanySlug = company.Slug,
            CompanyName = company.Name,
            ActorMembershipId = actorMembership.Id,
            ActorRoleId = actorMembership.RoleId,
            ActorRoleCode = actorMembership.RoleCode,
            ActorRoleLabel = actorMembership.RoleLabel,
            IsOwner = IsOwnerRole(actorMembership.RoleCode),
            IsAdmin = AdminRoleCodes.Contains(actorMembership.RoleCode),
            ValidFrom = actorMembership.ValidFrom,
            ValidTo = actorMembership.ValidTo
        });
    }

    private static CustomerWorkspaceModel ToCustomerWorkspace(
        Guid appUserId,
        App.DAL.DTO.Customers.CustomerWorkspaceDalDto customer)
    {
        return new CustomerWorkspaceModel
        {
            AppUserId = appUserId,
            ManagementCompanyId = customer.ManagementCompanyId,
            CompanySlug = customer.CompanySlug,
            CompanyName = customer.CompanyName,
            CustomerId = customer.Id,
            CustomerSlug = customer.Slug,
            CustomerName = customer.Name
        };
    }

    private static ResidentWorkspaceModel ToResidentWorkspace(
        Guid appUserId,
        ResidentProfileDalDto resident)
    {
        return new ResidentWorkspaceModel
        {
            AppUserId = appUserId,
            ManagementCompanyId = resident.ManagementCompanyId,
            CompanySlug = resident.CompanySlug,
            CompanyName = resident.CompanyName,
            ResidentId = resident.Id,
            ResidentIdCode = resident.IdCode,
            FirstName = resident.FirstName,
            LastName = resident.LastName,
            FullName = BuildFullName(resident.FirstName, resident.LastName),
            PreferredLanguage = resident.PreferredLanguage
        };
    }

    private static string BuildFullName(string firstName, string lastName)
    {
        return string.Join(
            " ",
            new[] { firstName, lastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static bool IsOwnerRole(string? roleCode)
    {
        return string.Equals(roleCode, OwnerRoleCode, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMembershipEffective(DateOnly validFrom, DateOnly? validTo, DateOnly today)
    {
        return validFrom <= today
               && (!validTo.HasValue || validTo.Value >= today);
    }

    private static Error WithAuthorizationReason(
        Error error,
        CompanyMembershipAuthorizationFailureReason reason)
    {
        error.Metadata["FailureReason"] = reason;
        return error;
    }
}
