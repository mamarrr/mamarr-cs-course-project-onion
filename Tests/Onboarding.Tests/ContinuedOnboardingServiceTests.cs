using System.Security.Claims;
using App.BLL.Onboarding;
using App.DAL.EF;
using App.Domain;
using App.Domain.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WebApp.Controllers;
using WebApp.Middleware;
using WebApp.ViewModels.Onboarding;
using Xunit;

namespace Onboarding.Tests;

public class ContinuedOnboardingServiceTests
{
    [Fact]
    public async Task HasAnyContextAsync_ReturnsTrue_WhenActiveManagementContextExists()
    {
        await using var dbContext = CreateDbContext();
        var appUserId = Guid.NewGuid();

        AddManagementContext(dbContext, appUserId, true);
        await dbContext.SaveChangesAsync();

        var sut = CreateService(dbContext);

        var result = await sut.HasAnyContextAsync(appUserId);

        Assert.True(result);
    }

    [Fact]
    public async Task HasAnyContextAsync_ReturnsTrue_WhenActiveResidentContextExists()
    {
        await using var dbContext = CreateDbContext();
        var appUserId = Guid.NewGuid();

        dbContext.ResidentUsers.Add(new ResidentUser
        {
            AppUserId = appUserId,
            ResidentId = Guid.NewGuid(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        await dbContext.SaveChangesAsync();

        var sut = CreateService(dbContext);

        var result = await sut.HasAnyContextAsync(appUserId);

        Assert.True(result);
    }

    [Fact]
    public async Task HasAnyContextAsync_ReturnsFalse_WhenOnlyInactiveContextsExist()
    {
        await using var dbContext = CreateDbContext();
        var appUserId = Guid.NewGuid();

        AddManagementContext(dbContext, appUserId, false);
        dbContext.ResidentUsers.Add(new ResidentUser
        {
            AppUserId = appUserId,
            ResidentId = Guid.NewGuid(),
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        await dbContext.SaveChangesAsync();

        var sut = CreateService(dbContext);

        var result = await sut.HasAnyContextAsync(appUserId);

        Assert.False(result);
    }

    [Fact]
    public async Task OnboardingContextGuard_AllowsSystemAdminWithoutRedirect()
    {
        var userManager = CreateUserManagerMock();
        var service = CreateService(CreateDbContext());
        var principal = BuildAuthenticatedPrincipal("SystemAdmin");

        var (nextCalled, location) = await InvokeMiddlewareAsync(principal, service, userManager.Object);

        Assert.True(nextCalled);
        Assert.Null(location);
    }

    [Fact]
    public async Task OnboardingContextGuard_AllowsManagementContextUserWithoutRedirect()
    {
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManagerMock();
        var appUser = new AppUser { Id = Guid.NewGuid(), Email = "mctx@test", UserName = "mctx@test" };

        AddManagementContext(dbContext, appUser.Id, true);
        await dbContext.SaveChangesAsync();

        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(appUser);
        var principal = BuildAuthenticatedPrincipal();
        var service = CreateService(dbContext);

        var (nextCalled, location) = await InvokeMiddlewareAsync(principal, service, userManager.Object);

        Assert.True(nextCalled);
        Assert.Null(location);
    }

    [Fact]
    public async Task OnboardingContextGuard_AllowsResidentContextUserWithoutRedirect()
    {
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManagerMock();
        var appUser = new AppUser { Id = Guid.NewGuid(), Email = "rctx@test", UserName = "rctx@test" };

        dbContext.ResidentUsers.Add(new ResidentUser
        {
            AppUserId = appUser.Id,
            ResidentId = Guid.NewGuid(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        await dbContext.SaveChangesAsync();

        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(appUser);
        var principal = BuildAuthenticatedPrincipal();
        var service = CreateService(dbContext);

        var (nextCalled, location) = await InvokeMiddlewareAsync(principal, service, userManager.Object);

        Assert.True(nextCalled);
        Assert.Null(location);
    }

    [Fact]
    public async Task OnboardingContextGuard_RedirectsAuthenticatedUserWithoutContextToOnboardingChooser()
    {
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManagerMock();
        var appUser = new AppUser { Id = Guid.NewGuid(), Email = "nocontext@test", UserName = "nocontext@test" };

        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(appUser);
        var principal = BuildAuthenticatedPrincipal();
        var service = CreateService(dbContext);

        var (nextCalled, location) = await InvokeMiddlewareAsync(principal, service, userManager.Object);

        Assert.False(nextCalled);
        Assert.Equal("/Onboarding", location);
    }

    [Fact]
    public async Task NewManagementCompanyPost_Success_CreatesCompanyAndLink_AndRedirectsToManagementDashboard()
    {
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);

        var appUser = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = "owner@test",
            Email = "owner@test",
            FirstName = "Owner",
            LastName = "User"
        };
        var ownerRole = new ManagementCompanyRole
        {
            Id = Guid.NewGuid(),
            Code = "OWNER",
            Label = "Owner"
        };

        dbContext.Users.Add(appUser);
        dbContext.ManagementCompanyRoles.Add(ownerRole);
        await dbContext.SaveChangesAsync();

        userManager.SetupGet(x => x.Users).Returns(dbContext.Users);
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(appUser);

        var onboardingService = new OnboardingService(userManager.Object, signInManager.Object, dbContext);
        var controller = new OnboardingController(onboardingService, userManager.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = BuildAuthenticatedPrincipal()
                }
            }
        };

        var vm = new CreateManagementCompanyViewModel
        {
            Name = "Test Management Company",
            RegistryCode = "REG-100",
            VatNumber = "VAT-100",
            Email = "company@test.com",
            Phone = "+372000000",
            Address = "Test street 1"
        };

        var result = await controller.NewManagementCompany(vm);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Dashboard", redirect.ControllerName);
        Assert.Equal("Management", redirect.RouteValues?["area"]);

        var company = await dbContext.ManagementCompanies.SingleAsync();
        var companyUser = await dbContext.ManagementCompanyUsers.SingleAsync();

        Assert.Equal("Test Management Company", company.Name);
        Assert.Equal("REG-100", company.RegistryCode);
        Assert.Equal(company.Id, companyUser.ManagementCompanyId);
        Assert.Equal(appUser.Id, companyUser.AppUserId);
        Assert.Equal(ownerRole.Id, companyUser.ManagementCompanyRoleId);
    }

    [Fact]
    public void OnboardingChooserView_ContainsThreeOptions_AndTwoPlaceholderStyleActions()
    {
        var root = FindRepoRoot();
        var viewPath = Path.Combine(root, "WebApp", "Views", "Onboarding", "Index.cshtml");
        var content = File.ReadAllText(viewPath);

        Assert.Contains("New management company", content);
        Assert.Contains("Management company employee", content);
        Assert.Contains("Resident", content);

        Assert.Equal(2, CountOccurrences(content, "btn btn-outline-secondary\">Open</a>"));
        Assert.Equal(1, CountOccurrences(content, "btn btn-primary\">Start setup</a>"));
    }

    private static async Task<(bool nextCalled, string? location)> InvokeMiddlewareAsync(
        ClaimsPrincipal user,
        IOnboardingService onboardingService,
        UserManager<AppUser> userManager)
    {
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new OnboardingContextGuardMiddleware(next);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/dashboard";
        httpContext.User = user;

        await middleware.InvokeAsync(httpContext, onboardingService, userManager);

        return (nextCalled, httpContext.Response.Headers.Location.ToString() is { Length: > 0 } location ? location : null);
    }

    private static ClaimsPrincipal BuildAuthenticatedPrincipal(params string[] roles)
    {
        var claims = new List<Claim>();
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        return new ClaimsPrincipal(identity);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    private static void AddManagementContext(AppDbContext dbContext, Guid appUserId, bool isActive)
    {
        var companyId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        dbContext.ManagementCompanies.Add(new ManagementCompany
        {
            Id = companyId,
            Name = $"Management Company {companyId}",
            RegistryCode = $"REG-{companyId:N}"[..16],
            VatNumber = $"VAT-{companyId:N}"[..16],
            Email = $"{companyId:N}@test.com",
            Phone = "00000000",
            Address = "Test address 1",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        dbContext.ManagementCompanyRoles.Add(new ManagementCompanyRole
        {
            Id = roleId,
            Code = $"OWNER-{roleId:N}"[..16],
            Label = "Owner"
        });

        dbContext.ManagementCompanyUsers.Add(new ManagementCompanyUser
        {
            ManagementCompanyId = companyId,
            ManagementCompanyRoleId = roleId,
            AppUserId = appUserId,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow),
            JobTitle = "Owner"
        });
    }

    private static OnboardingService CreateService(AppDbContext dbContext)
    {
        var userManager = CreateUserManagerMock();
        userManager.SetupGet(x => x.Users).Returns(dbContext.Users);
        var signInManager = CreateSignInManagerMock(userManager.Object);

        return new OnboardingService(userManager.Object, signInManager.Object, dbContext);
    }

    private static Mock<UserManager<AppUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(
            store.Object,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<AppUser>(),
            Array.Empty<IUserValidator<AppUser>>(),
            Array.Empty<IPasswordValidator<AppUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<AppUser>>>().Object);
    }

    private static Mock<SignInManager<AppUser>> CreateSignInManagerMock(UserManager<AppUser> userManager)
    {
        return new Mock<SignInManager<AppUser>>(
            userManager,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<AppUser>>().Object,
            Options.Create(new IdentityOptions()),
            new Mock<ILogger<SignInManager<AppUser>>>().Object,
            new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>().Object,
            new DefaultUserConfirmation<AppUser>());
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "mamarrproject.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root with mamarrproject.sln was not found.");
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;

        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
