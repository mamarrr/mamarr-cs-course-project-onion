using App.BLL.Contracts.Onboarding;
using WebApp.Services.Identity;

namespace WebApp.Middleware;

public class OnboardingContextGuardMiddleware
{
    private static readonly HashSet<string> StaticPrefixes =
    [
        "/css",
        "/js",
        "/lib",
        "/images",
        "/favicon"
    ];

    private readonly RequestDelegate _next;

    public OnboardingContextGuardMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IAccountOnboardingService accountOnboardingService,
        IIdentityAccountService identityAccountService)
    {
        var path = context.Request.Path;

        if (ShouldSkipPath(path))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        if (context.User.IsInRole("SystemAdmin"))
        {
            await _next(context);
            return;
        }

        var appUserId = await identityAccountService.GetAuthenticatedUserIdAsync(context.User, context.RequestAborted);
        if (appUserId == null)
        {
            await _next(context);
            return;
        }

        if (path.StartsWithSegments("/m", out var remainingManagementPath))
        {
            var companySlug = remainingManagementPath.Value?
                .Trim('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            var hasManagementAccess = !string.IsNullOrWhiteSpace(companySlug) &&
                                      await accountOnboardingService.UserHasManagementCompanyAccessAsync(
                                          appUserId.Value,
                                          companySlug,
                                          context.RequestAborted);

            if (!hasManagementAccess)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await _next(context);
            return;
        }

        var hasContext = await accountOnboardingService.HasAnyContextAsync(appUserId.Value, context.RequestAborted);
        if (hasContext)
        {
            await _next(context);
            return;
        }

        context.Response.Redirect("/onboarding");
    }

    private static bool ShouldSkipPath(PathString path)
    {
        if (!path.HasValue) return true;

        if (path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)) return true;
        if (path == PathString.FromUriComponent("/")) return true;
        if (path.StartsWithSegments("/Onboarding", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWithSegments("/onboarding", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWithSegments("/login", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWithSegments("/register", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWithSegments("/logout", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWithSegments("/set-language", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWithSegments("/set-context", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWithSegments("/privacy", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWithSegments("/home", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWithSegments("/access-denied", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWithSegments("/error", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWithSegments("/Home/SetLanguage", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWithSegments("/Identity", StringComparison.OrdinalIgnoreCase)) return true;
        if (path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase)) return true;

        if (StaticPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))) return true;

        var value = path.Value!;
        if (value.Contains('.', StringComparison.Ordinal)) return true;

        return false;
    }
}
