using System.Security.Claims;
using App.Domain.Identity;
using Base.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Helpers;

public static class JwtTestHelper
{
    public static async Task<string> GenerateTokenAsync(IServiceProvider rootServices, string email)
    {
        using var scope = rootServices.CreateScope();
        var sp = scope.ServiceProvider;

        var userManager = sp.GetRequiredService<UserManager<AppUser>>();
        var signInManager = sp.GetRequiredService<SignInManager<AppUser>>();
        var configuration = sp.GetRequiredService<IConfiguration>();

        var user = await userManager.FindByEmailAsync(email)
                   ?? throw new InvalidOperationException($"Test user {email} not found in DB");

        var principal = await signInManager.CreateUserPrincipalAsync(user);

        return IdentityHelpers.GenerateJwt(
            principal.Claims,
            configuration["JWT:Key"]!,
            configuration["JWT:Issuer"]!,
            configuration["JWT:Audience"]!,
            configuration.GetValue<int>("JWT:ExpiresInSeconds"));
    }

    public static string GenerateTokenForUserId(IConfiguration configuration, Guid userId, string email)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, email),
        };

        return IdentityHelpers.GenerateJwt(
            claims,
            configuration["JWT:Key"]!,
            configuration["JWT:Issuer"]!,
            configuration["JWT:Audience"]!,
            configuration.GetValue<int>("JWT:ExpiresInSeconds"));
    }
}
