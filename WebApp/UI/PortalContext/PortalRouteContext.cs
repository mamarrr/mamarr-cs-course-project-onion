namespace WebApp.UI.PortalContext;

public sealed class PortalRouteContext
{
    public Guid? AppUserId { get; init; }
    public string? CompanySlug { get; init; }
    public string? CustomerSlug { get; init; }
    public string? PropertySlug { get; init; }
    public string? UnitSlug { get; init; }
    public string? ResidentIdCode { get; init; }
    public PortalContextKind Kind { get; init; }

    public bool IsAuthenticated => AppUserId.HasValue;
}

