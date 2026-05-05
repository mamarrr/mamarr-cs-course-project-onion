using System.Security.Claims;
using Microsoft.AspNetCore.Routing;

namespace WebApp.UI.PortalContext;

public sealed class CurrentPortalContextResolver : ICurrentPortalContextResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentPortalContextResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public PortalRouteContext Resolve()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var routeValues = httpContext?.Request.RouteValues;

        var companySlug = RouteString(routeValues, "companySlug");
        var customerSlug = RouteString(routeValues, "customerSlug");
        var propertySlug = RouteString(routeValues, "propertySlug");
        var unitSlug = RouteString(routeValues, "unitSlug");
        var residentIdCode = RouteString(routeValues, "residentIdCode");

        return new PortalRouteContext
        {
            AppUserId = ResolveAppUserId(httpContext?.User),
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UnitSlug = unitSlug,
            ResidentIdCode = residentIdCode,
            Kind = ResolveKind(companySlug, customerSlug, propertySlug, unitSlug, residentIdCode)
        };
    }

    private static Guid? ResolveAppUserId(ClaimsPrincipal? user)
    {
        var userIdValue = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : null;
    }

    private static string? RouteString(RouteValueDictionary? routeValues, string key)
    {
        if (routeValues is null || !routeValues.TryGetValue(key, out var value))
        {
            return null;
        }

        var text = value?.ToString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static PortalContextKind ResolveKind(
        string? companySlug,
        string? customerSlug,
        string? propertySlug,
        string? unitSlug,
        string? residentIdCode)
    {
        if (!string.IsNullOrWhiteSpace(unitSlug)) return PortalContextKind.Unit;
        if (!string.IsNullOrWhiteSpace(propertySlug)) return PortalContextKind.Property;
        if (!string.IsNullOrWhiteSpace(customerSlug)) return PortalContextKind.Customer;
        if (!string.IsNullOrWhiteSpace(residentIdCode)) return PortalContextKind.Resident;
        return string.IsNullOrWhiteSpace(companySlug)
            ? PortalContextKind.Unknown
            : PortalContextKind.ManagementCompany;
    }
}
