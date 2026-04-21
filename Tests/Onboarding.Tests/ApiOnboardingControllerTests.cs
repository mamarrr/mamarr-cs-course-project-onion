using System.Net;
using App.BLL.Onboarding;
using App.BLL.Onboarding.Account;
using App.BLL.Onboarding.Api;
using App.DAL.EF;
using App.Domain;
using App.Domain.Identity;
using App.DTO.v1.Onboarding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WebApp.ApiControllers.Onboarding;
using WebApp.ApiControllers.Shared;
using Xunit;

namespace Onboarding.Tests;

public class ApiOnboardingControllerTests
{
    [Fact]
    public async Task GetContexts_ReturnsAvailableManagementContext()
    {
        await using var dbContext = CreateDbContext(nameof(GetContexts_ReturnsAvailableManagementContext));
        var userId = Guid.NewGuid();
        SeedManagementContext(dbContext, userId, "acme", "Acme Management");

        var sut = CreateController(dbContext, userId);

        var result = await sut.GetContexts(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<OnboardingContextsResponseDto>(ok.Value);
        var context = Assert.Single(dto.Contexts);
        Assert.Equal("management", context.ContextType);
        Assert.Equal("acme", context.RouteContext.CompanySlug);
        Assert.Equal("management-dashboard", context.RouteContext.CurrentSection);
        Assert.True(dto.DefaultContext?.IsDefault);
    }

    [Fact]
    public async Task CreateManagementCompany_WithInvalidModel_ReturnsBadRequest()
    {
        await using var dbContext = CreateDbContext(nameof(CreateManagementCompany_WithInvalidModel_ReturnsBadRequest));
        var userId = Guid.NewGuid();

        var sut = CreateController(dbContext, userId);
        sut.ModelState.AddModelError(nameof(CreateManagementCompanyRequestDto.Name), "The Name field is required.");

        var result = await sut.CreateManagementCompany(new CreateManagementCompanyRequestDto());

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal((int)HttpStatusCode.BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task CreateManagementCompany_CreatesCompanyAndReturnsCreated()
    {
        await using var dbContext = CreateDbContext(nameof(CreateManagementCompany_CreatesCompanyAndReturnsCreated));
        var userId = Guid.NewGuid();

        var sut = CreateController(dbContext, userId);

        var result = await sut.CreateManagementCompany(new CreateManagementCompanyRequestDto
        {
            Name = "North Star Management",
            RegistryCode = "REG-100",
            VatNumber = "VAT-100",
            Email = "northstar@example.com",
            Phone = "+37255550000",
            Address = "Tallinn 1"
        });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<CreateManagementCompanyResponseDto>(created.Value);
        Assert.Equal("management-dashboard", dto.RouteContext.CurrentSection);
        Assert.False(Guid.Empty == dto.ManagementCompanyId);

        var company = await dbContext.ManagementCompanies.SingleAsync();
        Assert.Equal("North Star Management", company.Name);
        Assert.Equal(dto.ManagementCompanySlug, company.Slug);
        Assert.Single(dbContext.ManagementCompanyUsers);
    }

    private static OnboardingController CreateController(AppDbContext dbContext, Guid userId)
    {
        var appUser = new AppUser
        {
            Id = userId,
            Email = "user@example.com",
            UserName = "user@example.com",
            FirstName = "Api",
            LastName = "User"
        };
        dbContext.Users.Add(appUser);
        if (!dbContext.ManagementCompanyRoles.Any(x => x.Code == "OWNER"))
        {
            dbContext.ManagementCompanyRoles.Add(new ManagementCompanyRole
            {
                Id = Guid.NewGuid(),
                Code = "OWNER",
                Label = "Owner"
            });
        }
        dbContext.SaveChanges();

        var userManager = CreateUserManagerMock(dbContext, appUser).Object;
        var onboardingService = new AccountOnboardingService(userManager, CreateSignInManagerMock(userManager).Object, dbContext);
        var apiContextService = new ApiWorkspaceContextService(dbContext, onboardingService);
        var routeContextMapper = new ApiOnboardingRouteContextMapper();

        var sut = new OnboardingController(apiContextService, routeContextMapper, onboardingService, userManager)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return sut;
    }

    private static void SeedManagementContext(AppDbContext dbContext, Guid userId, string slug, string name)
    {
        var companyId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        dbContext.ManagementCompanyRoles.Add(new ManagementCompanyRole
        {
            Id = roleId,
            Code = "OWNER",
            Label = "Owner"
        });

        dbContext.ManagementCompanies.Add(new ManagementCompany
        {
            Id = companyId,
            Name = name,
            Slug = slug,
            RegistryCode = $"REG-{slug}",
            VatNumber = $"VAT-{slug}",
            Email = $"{slug}@example.com",
            Phone = "+37200000000",
            Address = "Address",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });

        dbContext.ManagementCompanyUsers.Add(new ManagementCompanyUser
        {
            Id = Guid.NewGuid(),
            AppUserId = userId,
            ManagementCompanyId = companyId,
            ManagementCompanyRoleId = roleId,
            JobTitle = "Owner",
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        dbContext.SaveChanges();
    }

    private static AppDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static Mock<UserManager<AppUser>> CreateUserManagerMock(AppDbContext dbContext, AppUser appUser)
    {
        var store = new Mock<IUserStore<AppUser>>();
        var mock = new Mock<UserManager<AppUser>>(
            store.Object,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<AppUser>(),
            Array.Empty<IUserValidator<AppUser>>(),
            Array.Empty<IPasswordValidator<AppUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<AppUser>>>().Object);

        mock.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(appUser);

        mock.Setup(x => x.Users)
            .Returns(dbContext.Users);

        return mock;
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
}
