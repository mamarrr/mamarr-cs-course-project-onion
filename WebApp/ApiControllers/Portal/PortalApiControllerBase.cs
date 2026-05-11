using App.BLL.DTO.Common.Routes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Portal;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public abstract class PortalApiControllerBase : ApiControllerBase
{
    protected ActionResult<ManagementCompanyRoute> CompanyRoute(string companySlug)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        return new ManagementCompanyRoute
        {
            AppUserId = appUserId.Value,
            CompanySlug = companySlug
        };
    }

    protected ActionResult<CustomerRoute> CustomerRoute(string companySlug, string customerSlug)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        return new CustomerRoute
        {
            AppUserId = appUserId.Value,
            CompanySlug = companySlug,
            CustomerSlug = customerSlug
        };
    }

    protected ActionResult<PropertyRoute> PropertyRoute(
        string companySlug,
        string customerSlug,
        string propertySlug)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        return new PropertyRoute
        {
            AppUserId = appUserId.Value,
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug
        };
    }

    protected ActionResult<UnitRoute> UnitRoute(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        return new UnitRoute
        {
            AppUserId = appUserId.Value,
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UnitSlug = unitSlug
        };
    }

    protected ActionResult<ResidentRoute> ResidentRoute(string companySlug, string residentIdCode)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        return new ResidentRoute
        {
            AppUserId = appUserId.Value,
            CompanySlug = companySlug,
            ResidentIdCode = residentIdCode
        };
    }

    protected ActionResult<ResidentContactRoute> ResidentContactRoute(
        string companySlug,
        string residentIdCode,
        Guid residentContactId)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        return new ResidentContactRoute
        {
            AppUserId = appUserId.Value,
            CompanySlug = companySlug,
            ResidentIdCode = residentIdCode,
            ResidentContactId = residentContactId
        };
    }

    protected ActionResult<ResidentLeaseRoute> ResidentLeaseRoute(
        string companySlug,
        string residentIdCode,
        Guid leaseId)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        return new ResidentLeaseRoute
        {
            AppUserId = appUserId.Value,
            CompanySlug = companySlug,
            ResidentIdCode = residentIdCode,
            LeaseId = leaseId
        };
    }

    protected ActionResult<UnitLeaseRoute> UnitLeaseRoute(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        Guid leaseId)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        return new UnitLeaseRoute
        {
            AppUserId = appUserId.Value,
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UnitSlug = unitSlug,
            LeaseId = leaseId
        };
    }

    protected ActionResult<VendorRoute> VendorRoute(string companySlug, Guid vendorId)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        return new VendorRoute
        {
            AppUserId = appUserId.Value,
            CompanySlug = companySlug,
            VendorId = vendorId
        };
    }

    protected ActionResult<VendorContactRoute> VendorContactRoute(
        string companySlug,
        Guid vendorId,
        Guid vendorContactId)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        return new VendorContactRoute
        {
            AppUserId = appUserId.Value,
            CompanySlug = companySlug,
            VendorId = vendorId,
            VendorContactId = vendorContactId
        };
    }
}
