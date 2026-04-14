namespace WebApp.Middleware;

public static class OnboardingContextGuardExtensions
{
    public static IApplicationBuilder UseOnboardingContextGuard(this IApplicationBuilder app)
    {
        return app.UseMiddleware<OnboardingContextGuardMiddleware>();
    }
}
