using App.DAL.EF;
using App.Domain;
using App.Domain.Identity;
using Base.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Helpers;

public static class DataSeeder
{
    public static void SeedData(IServiceProvider rootServices)
    {
        SeedAsync(rootServices).GetAwaiter().GetResult();
    }

    public static async Task SeedAsync(IServiceProvider rootServices)
    {
        using var scope = rootServices.CreateScope();
        var sp = scope.ServiceProvider;

        var userManager = sp.GetRequiredService<UserManager<AppUser>>();
        var ctx = sp.GetRequiredService<AppDbContext>();

        await EnsureUserAsync(userManager, TestUsers.UserAId, TestUsers.UserAEmail, TestUsers.Password);
        await EnsureUserAsync(userManager, TestUsers.UserBId, TestUsers.UserBEmail, TestUsers.Password);

        await SeedItemsAsync(ctx, TestUsers.UserAId, TestUsers.UserBId);
    }

    public static async Task SeedItemsAsync(AppDbContext ctx, Guid userAId, Guid userBId)
    {

        await ctx.SaveChangesAsync();
    }

    private static async Task EnsureUserAsync(UserManager<AppUser> userManager, Guid id, string email, string password)
    {
    }
}
