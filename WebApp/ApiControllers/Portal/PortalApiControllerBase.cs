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
}
