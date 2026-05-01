using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WebApp.UI.Workspace;

namespace WebApp.UI.Chrome;

public class AppChromeRequest
{
    public ClaimsPrincipal User { get; init; } = default!;

    public HttpContext HttpContext { get; init; } = default!;

    public string PageTitle { get; init; } = string.Empty;

    public string ActiveSection { get; init; } = string.Empty;

    public string? ManagementCompanySlug { get; init; }

    public string? ManagementCompanyName { get; init; }

    public string? CustomerSlug { get; init; }

    public string? CustomerName { get; init; }

    public string? PropertySlug { get; init; }

    public string? PropertyName { get; init; }

    public string? UnitSlug { get; init; }

    public string? UnitName { get; init; }

    public string? ResidentIdCode { get; init; }

    public string? ResidentDisplayName { get; init; }

    public string? ResidentSupportingText { get; init; }

    public WorkspaceLevel CurrentLevel { get; init; } = WorkspaceLevel.None;
}
