using System.Security.Claims;
using App.BLL.ManagementUsers;
using App.BLL.Onboarding;
using App.BLL.Routing;
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
    public async Task ManagementCompanyJoinRequestService_CreateJoinRequest_Succeeds_ForValidInput()
    {
        await using var dbContext = CreateDbContext();
        var requester = CreateUser("requester@test.com", "Requester", "User");
        var company = CreateCompany("north-estate", "North Estate");
        var managerRole = new ManagementCompanyRole { Id = Guid.NewGuid(), Code = "MANAGER", Label = "Manager" };

        dbContext.Users.Add(requester);
        dbContext.ManagementCompanies.Add(company);
        dbContext.ManagementCompanyRoles.Add(managerRole);
        await dbContext.SaveChangesAsync();

        var sut = new ManagementCompanyJoinRequestService(dbContext);
        var result = await sut.CreateJoinRequestAsync(new CreateManagementCompanyJoinRequest
        {
            AppUserId = requester.Id,
            RegistryCode = company.RegistryCode,
            RequestedRoleId = managerRole.Id,
            Message = "I can help with operations"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.RequestId);
        Assert.True(await dbContext.ManagementCompanyJoinRequests.AnyAsync(x => x.Id == result.RequestId));
    }

    [Fact]
    public async Task ManagementCompanyJoinRequestService_CreateJoinRequest_BlocksDuplicatePending()
    {
        await using var dbContext = CreateDbContext();
        var requester = CreateUser("requester@test.com", "Requester", "User");
        var company = CreateCompany("north-estate", "North Estate");
        var managerRole = new ManagementCompanyRole { Id = Guid.NewGuid(), Code = "MANAGER", Label = "Manager" };

        dbContext.Users.Add(requester);
        dbContext.ManagementCompanies.Add(company);
        dbContext.ManagementCompanyRoles.Add(managerRole);
        dbContext.ManagementCompanyJoinRequests.Add(new ManagementCompanyJoinRequest
        {
            Id = Guid.NewGuid(),
            AppUserId = requester.Id,
            ManagementCompanyId = company.Id,
            RequestedManagementCompanyRoleId = managerRole.Id,
            Status = ManagementCompanyJoinRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var sut = new ManagementCompanyJoinRequestService(dbContext);
        var result = await sut.CreateJoinRequestAsync(new CreateManagementCompanyJoinRequest
        {
            AppUserId = requester.Id,
            RegistryCode = company.RegistryCode,
            RequestedRoleId = managerRole.Id
        });

        Assert.True(result.DuplicatePendingRequest);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ManagementCompanyJoinRequestService_CreateJoinRequest_BlocksAlreadyMember()
    {
        await using var dbContext = CreateDbContext();
        var requester = CreateUser("requester@test.com", "Requester", "User");
        var company = CreateCompany("north-estate", "North Estate");
        var managerRole = new ManagementCompanyRole { Id = Guid.NewGuid(), Code = "MANAGER", Label = "Manager" };

        dbContext.Users.Add(requester);
        dbContext.ManagementCompanies.Add(company);
        dbContext.ManagementCompanyRoles.Add(managerRole);
        dbContext.ManagementCompanyUsers.Add(new ManagementCompanyUser
        {
            Id = Guid.NewGuid(),
            AppUserId = requester.Id,
            ManagementCompanyId = company.Id,
            ManagementCompanyRoleId = managerRole.Id,
            JobTitle = "Manager",
            IsActive = true,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var sut = new ManagementCompanyJoinRequestService(dbContext);
        var result = await sut.CreateJoinRequestAsync(new CreateManagementCompanyJoinRequest
        {
            AppUserId = requester.Id,
            RegistryCode = company.RegistryCode,
            RequestedRoleId = managerRole.Id
        });

        Assert.True(result.AlreadyMember);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ManagementCompanyJoinRequestService_RejectsApproval_ForUnauthorizedActor()
    {
        await using var dbContext = CreateDbContext();
        var actor = CreateUser("support@test.com", "Support", "User");
        var requester = CreateUser("requester@test.com", "Requester", "User");
        var company = CreateCompany("north-estate", "North Estate");
        var supportRole = new ManagementCompanyRole { Id = Guid.NewGuid(), Code = "SUPPORT", Label = "Support" };
        var managerRole = new ManagementCompanyRole { Id = Guid.NewGuid(), Code = "MANAGER", Label = "Manager" };

        dbContext.Users.AddRange(actor, requester);
        dbContext.ManagementCompanies.Add(company);
        dbContext.ManagementCompanyRoles.AddRange(supportRole, managerRole);
        dbContext.ManagementCompanyUsers.Add(new ManagementCompanyUser
        {
            Id = Guid.NewGuid(),
            AppUserId = actor.Id,
            ManagementCompanyId = company.Id,
            ManagementCompanyRoleId = supportRole.Id,
            JobTitle = "Support",
            IsActive = true,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTime.UtcNow
        });
        var request = new ManagementCompanyJoinRequest
        {
            Id = Guid.NewGuid(),
            AppUserId = requester.Id,
            ManagementCompanyId = company.Id,
            RequestedManagementCompanyRoleId = managerRole.Id,
            Status = ManagementCompanyJoinRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.ManagementCompanyJoinRequests.Add(request);
        await dbContext.SaveChangesAsync();

        var sut = new ManagementCompanyJoinRequestService(dbContext);
        var result = await sut.ApproveRequestAsync(actor.Id, company.Id, request.Id);

        Assert.True(result.Forbidden);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ManagementCompanyJoinRequestService_CannotResolveRequestTwice()
    {
        await using var dbContext = CreateDbContext();
        var actor = CreateUser("owner@test.com", "Owner", "User");
        var requester = CreateUser("requester@test.com", "Requester", "User");
        var company = CreateCompany("north-estate", "North Estate");
        var ownerRole = new ManagementCompanyRole { Id = Guid.NewGuid(), Code = "OWNER", Label = "Owner" };
        var managerRole = new ManagementCompanyRole { Id = Guid.NewGuid(), Code = "MANAGER", Label = "Manager" };

        dbContext.Users.AddRange(actor, requester);
        dbContext.ManagementCompanies.Add(company);
        dbContext.ManagementCompanyRoles.AddRange(ownerRole, managerRole);
        dbContext.ManagementCompanyUsers.Add(new ManagementCompanyUser
        {
            Id = Guid.NewGuid(),
            AppUserId = actor.Id,
            ManagementCompanyId = company.Id,
            ManagementCompanyRoleId = ownerRole.Id,
            JobTitle = "Owner",
            IsActive = true,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTime.UtcNow
        });
        var request = new ManagementCompanyJoinRequest
        {
            Id = Guid.NewGuid(),
            AppUserId = requester.Id,
            ManagementCompanyId = company.Id,
            RequestedManagementCompanyRoleId = managerRole.Id,
            Status = ManagementCompanyJoinRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.ManagementCompanyJoinRequests.Add(request);
        await dbContext.SaveChangesAsync();

        var sut = new ManagementCompanyJoinRequestService(dbContext);
        var first = await sut.RejectRequestAsync(actor.Id, company.Id, request.Id);
        var second = await sut.RejectRequestAsync(actor.Id, company.Id, request.Id);

        Assert.True(first.Success);
        Assert.True(second.AlreadyResolved);
    }

    [Fact]
    public async Task ManagementCompanyJoinRequestService_BlocksCrossTenantApproval_ByNotFound()
    {
        await using var dbContext = CreateDbContext();
        var actor = CreateUser("owner@test.com", "Owner", "User");
        var requester = CreateUser("requester@test.com", "Requester", "User");
        var companyA = CreateCompany("north-estate", "North Estate");
        var companyB = CreateCompany("south-estate", "South Estate");
        var ownerRole = new ManagementCompanyRole { Id = Guid.NewGuid(), Code = "OWNER", Label = "Owner" };
        var managerRole = new ManagementCompanyRole { Id = Guid.NewGuid(), Code = "MANAGER", Label = "Manager" };

        dbContext.Users.AddRange(actor, requester);
        dbContext.ManagementCompanies.AddRange(companyA, companyB);
        dbContext.ManagementCompanyRoles.AddRange(ownerRole, managerRole);
        dbContext.ManagementCompanyUsers.Add(new ManagementCompanyUser
        {
            Id = Guid.NewGuid(),
            AppUserId = actor.Id,
            ManagementCompanyId = companyA.Id,
            ManagementCompanyRoleId = ownerRole.Id,
            JobTitle = "Owner",
            IsActive = true,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTime.UtcNow
        });
        var requestInB = new ManagementCompanyJoinRequest
        {
            Id = Guid.NewGuid(),
            AppUserId = requester.Id,
            ManagementCompanyId = companyB.Id,
            RequestedManagementCompanyRoleId = managerRole.Id,
            Status = ManagementCompanyJoinRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.ManagementCompanyJoinRequests.Add(requestInB);
        await dbContext.SaveChangesAsync();

        var sut = new ManagementCompanyJoinRequestService(dbContext);
        var result = await sut.ApproveRequestAsync(actor.Id, companyA.Id, requestInB.Id);

        Assert.True(result.NotFound);
        Assert.False(result.Success);
    }

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
    public void SlugGenerator_GeneratesAsciiSlug_AndEnsuresUniqueness()
    {
        var baseSlug = SlugGenerator.GenerateSlug("Äri Haldus & Partnerid");
        var uniqueSlug = SlugGenerator.EnsureUniqueSlug(baseSlug, [baseSlug, "other-company"]);

        Assert.Equal("ari-haldus-partnerid", baseSlug);
        Assert.Equal("ari-haldus-partnerid-2", uniqueSlug);
    }

    [Fact]
    public async Task GetDefaultManagementCompanySlugAsync_ReturnsAlphabeticallyFirstAccessibleSlug()
    {
        await using var dbContext = CreateDbContext();
        var appUserId = Guid.NewGuid();

        AddManagementContext(dbContext, appUserId, true, "beta-company", "Beta Company");
        AddManagementContext(dbContext, appUserId, true, "alpha-company", "Alpha Company");
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var slug = await service.GetDefaultManagementCompanySlugAsync(appUserId);

        Assert.Equal("alpha-company", slug);
    }

    [Fact]
    public async Task UserHasManagementCompanyAccessAsync_ReturnsTrue_OnlyForAuthorizedSlug()
    {
        await using var dbContext = CreateDbContext();
        var appUserId = Guid.NewGuid();

        AddManagementContext(dbContext, appUserId, true, "north-estate", "North Estate");
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        Assert.True(await service.UserHasManagementCompanyAccessAsync(appUserId, "north-estate"));
        Assert.False(await service.UserHasManagementCompanyAccessAsync(appUserId, "south-estate"));
    }

    [Fact]
    public async Task OnboardingContextService_ResolveContextRedirectAsync_ReturnsSelectedManagementDashboard()
    {
        await using var dbContext = CreateDbContext();
        var appUserId = Guid.NewGuid();

        AddManagementContext(dbContext, appUserId, true, "north-estate", "North Estate");
        await dbContext.SaveChangesAsync();

        var onboardingService = CreateService(dbContext);
        var contextService = new OnboardingContextService(dbContext, onboardingService);

        var result = await contextService.ResolveContextRedirectAsync(
            appUserId,
            new OnboardingContextSelectionCookieState
            {
                ContextType = "management",
                ManagementCompanySlug = "north-estate"
            });

        Assert.NotNull(result);
        Assert.Equal(OnboardingContextRedirectDestination.ManagementDashboard, result!.Destination);
        Assert.Equal("north-estate", result.CompanySlug);
    }

    [Fact]
    public async Task OnboardingContextService_AuthorizeContextSelectionAsync_DeniesCrossTenantManagementContext()
    {
        await using var dbContext = CreateDbContext();
        var appUserId = Guid.NewGuid();

        AddManagementContext(dbContext, appUserId, true, "allowed-company", "Allowed Company");
        await dbContext.SaveChangesAsync();

        var onboardingService = CreateService(dbContext);
        var contextService = new OnboardingContextService(dbContext, onboardingService);
        var unauthorizedCompanyId = Guid.NewGuid();

        var result = await contextService.AuthorizeContextSelectionAsync(appUserId, "management", unauthorizedCompanyId);

        Assert.False(result.Authorized);
        Assert.Equal("management", result.NormalizedType);
    }

    [Fact]
    public async Task OnboardingContextGuard_AllowsSystemAdminWithoutRedirect()
    {
        var userManager = CreateUserManagerMock();
        var service = CreateService(CreateDbContext());
        var principal = BuildAuthenticatedPrincipal("SystemAdmin");

        var result = await InvokeMiddlewareAsync(principal, service, userManager.Object, "/dashboard");

        Assert.True(result.nextCalled);
        Assert.Null(result.location);
        Assert.Equal(StatusCodes.Status200OK, result.statusCode);
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

        var result = await InvokeMiddlewareAsync(principal, service, userManager.Object, "/dashboard");

        Assert.True(result.nextCalled);
        Assert.Null(result.location);
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

        var result = await InvokeMiddlewareAsync(principal, service, userManager.Object, "/dashboard");

        Assert.True(result.nextCalled);
        Assert.Null(result.location);
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

        var result = await InvokeMiddlewareAsync(principal, service, userManager.Object, "/dashboard");

        Assert.False(result.nextCalled);
        Assert.Equal("/Onboarding", result.location);
    }

    [Fact]
    public async Task OnboardingIndex_ReturnsChooserView_ForAuthenticatedUserWithoutContext()
    {
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var appUser = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "chooser@test",
            UserName = "chooser@test",
            FirstName = "Chooser",
            LastName = "User"
        };

        dbContext.Users.Add(appUser);
        await dbContext.SaveChangesAsync();

        userManager.SetupGet(x => x.Users).Returns(dbContext.Users);
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(appUser);

        var onboardingService = new OnboardingService(userManager.Object, signInManager.Object, dbContext);
        var onboardingContextService = new OnboardingContextService(dbContext, onboardingService);
        var controller = new OnboardingController(
            onboardingService,
            onboardingContextService,
            CreateJoinRequestServiceMock().Object,
            CreateManagementUserAdminServiceMock().Object,
            userManager.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = BuildAuthenticatedPrincipal()
                }
            }
        };

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Null(view.ViewName);
        Assert.IsType<FlowChooserViewModel>(view.Model);
    }

    [Fact]
    public async Task OnboardingIndex_RedirectsAuthenticatedUserWithManagementContext_ToManagementSlugRoute()
    {
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);
        var appUser = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "manager@test",
            UserName = "manager@test",
            FirstName = "Manager",
            LastName = "User"
        };

        dbContext.Users.Add(appUser);
        AddManagementContext(dbContext, appUser.Id, true, "north-estate", "North Estate");
        await dbContext.SaveChangesAsync();

        userManager.SetupGet(x => x.Users).Returns(dbContext.Users);
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(appUser);

        var onboardingService = new OnboardingService(userManager.Object, signInManager.Object, dbContext);
        var onboardingContextService = new OnboardingContextService(dbContext, onboardingService);
        var httpContext = new DefaultHttpContext
        {
            User = BuildAuthenticatedPrincipal()
        };
        var controller = new OnboardingController(
            onboardingService,
            onboardingContextService,
            CreateJoinRequestServiceMock().Object,
            CreateManagementUserAdminServiceMock().Object,
            userManager.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        var result = await controller.Index();

        var redirect = Assert.IsType<RedirectToRouteResult>(result);
        Assert.Equal("management_dashboard", redirect.RouteName);
        Assert.Equal("north-estate", redirect.RouteValues?["companySlug"]);
    }

    [Fact]
    public async Task OnboardingContextGuard_ReturnsNotFound_ForUnauthorizedManagementSlug()
    {
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManagerMock();
        var appUser = new AppUser { Id = Guid.NewGuid(), Email = "mctx@test", UserName = "mctx@test" };

        AddManagementContext(dbContext, appUser.Id, true, "allowed-company", "Allowed Company");
        await dbContext.SaveChangesAsync();

        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(appUser);
        var principal = BuildAuthenticatedPrincipal();
        var service = CreateService(dbContext);

        var result = await InvokeMiddlewareAsync(principal, service, userManager.Object, "/m/forbidden-company");

        Assert.False(result.nextCalled);
        Assert.Null(result.location);
        Assert.Equal(StatusCodes.Status404NotFound, result.statusCode);
    }

    [Fact]
    public async Task NewManagementCompanyPost_Success_CreatesCompanyAndLink_AndRedirectsToManagementSlugRoute()
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
        var onboardingContextService = new OnboardingContextService(dbContext, onboardingService);
        var httpContext = new DefaultHttpContext
        {
            User = BuildAuthenticatedPrincipal()
        };

        var controller = new OnboardingController(
            onboardingService,
            onboardingContextService,
            CreateJoinRequestServiceMock().Object,
            CreateManagementUserAdminServiceMock().Object,
            userManager.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
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

        var redirect = Assert.IsType<RedirectToRouteResult>(result);
        Assert.Equal("management_dashboard", redirect.RouteName);
        Assert.Equal("test-management-company", redirect.RouteValues?["companySlug"]);

        var company = await dbContext.ManagementCompanies.SingleAsync();
        var companyUser = await dbContext.ManagementCompanyUsers.SingleAsync();

        Assert.Equal("Test Management Company", company.Name);
        Assert.Equal("test-management-company", company.Slug);
        Assert.Equal("REG-100", company.RegistryCode);
        Assert.Equal(company.Id, companyUser.ManagementCompanyId);
        Assert.Equal(appUser.Id, companyUser.AppUserId);
        Assert.Equal(ownerRole.Id, companyUser.ManagementCompanyRoleId);
        Assert.Contains("ctx.type=management", httpContext.Response.Headers["Set-Cookie"].ToString(), StringComparison.Ordinal);
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

    private static async Task<(bool nextCalled, string? location, int statusCode)> InvokeMiddlewareAsync(
        ClaimsPrincipal user,
        IOnboardingService onboardingService,
        UserManager<AppUser> userManager,
        string path)
    {
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new OnboardingContextGuardMiddleware(next);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;
        httpContext.User = user;

        await middleware.InvokeAsync(httpContext, onboardingService, userManager);

        return (
            nextCalled,
            httpContext.Response.Headers.Location.ToString() is { Length: > 0 } location ? location : null,
            httpContext.Response.StatusCode);
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

    private static void AddManagementContext(AppDbContext dbContext, Guid appUserId, bool isActive, string? slug = null, string? companyName = null)
    {
        var companyId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var resolvedCompanyName = companyName ?? $"Management Company {companyId}";
        var resolvedSlug = slug ?? SlugGenerator.GenerateSlug(resolvedCompanyName);

        dbContext.ManagementCompanies.Add(new ManagementCompany
        {
            Id = companyId,
            Name = resolvedCompanyName,
            Slug = resolvedSlug,
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

    private static Mock<IManagementCompanyJoinRequestService> CreateJoinRequestServiceMock()
    {
        return new Mock<IManagementCompanyJoinRequestService>();
    }

    private static Mock<IManagementUserAdminService> CreateManagementUserAdminServiceMock()
    {
        var mock = new Mock<IManagementUserAdminService>();
        mock.Setup(x => x.GetAvailableRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ManagementCompanyRole>());
        return mock;
    }

    private static AppUser CreateUser(string email, string firstName, string lastName)
    {
        return new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };
    }

    private static ManagementCompany CreateCompany(string slug, string name)
    {
        return new ManagementCompany
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = name,
            RegistryCode = $"REG-{Guid.NewGuid():N}"[..16],
            VatNumber = $"VAT-{Guid.NewGuid():N}"[..16],
            Email = $"{slug}@test.com",
            Phone = "+3720000000",
            Address = "Test address 1",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
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
