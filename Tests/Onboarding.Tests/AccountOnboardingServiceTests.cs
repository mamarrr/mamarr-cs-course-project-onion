using App.BLL.Onboarding;
using App.BLL.Onboarding.Account;
using App.Domain.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Onboarding.Tests;

public class AccountOnboardingServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesUserWithFirstAndLastName()
    {
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);

        userManager
            .Setup(m => m.FindByEmailAsync("new@user.test"))
            .ReturnsAsync((AppUser?)null);

        AppUser? createdUser = null;
        userManager
            .Setup(m => m.CreateAsync(It.IsAny<AppUser>(), "Pass123!"))
            .Callback<AppUser, string>((user, _) => createdUser = user)
            .ReturnsAsync(IdentityResult.Success);

        var sut = new AccountOnboardingService(userManager.Object, signInManager.Object);

        var result = await sut.RegisterAsync(new AccountRegisterRequest
        {
            Email = "new@user.test",
            Password = "Pass123!",
            FirstName = "Jane",
            LastName = "Doe"
        });

        Assert.True(result.Succeeded);
        Assert.NotNull(createdUser);
        Assert.Equal("Jane", createdUser!.FirstName);
        Assert.Equal("Doe", createdUser.LastName);
        Assert.Equal("new@user.test", createdUser.Email);
        Assert.Equal("new@user.test", createdUser.UserName);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailExists_ReturnsFailure()
    {
        var userManager = CreateUserManagerMock();
        var signInManager = CreateSignInManagerMock(userManager.Object);

        userManager
            .Setup(m => m.FindByEmailAsync("existing@user.test"))
            .ReturnsAsync(new AppUser { Email = "existing@user.test", UserName = "existing@user.test", FirstName = "Ex", LastName = "User" });

        var sut = new AccountOnboardingService(userManager.Object, signInManager.Object);

        var result = await sut.RegisterAsync(new AccountRegisterRequest
        {
            Email = "existing@user.test",
            Password = "Pass123!",
            FirstName = "Any",
            LastName = "Name"
        });

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Contains("already exists", StringComparison.OrdinalIgnoreCase));
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
}

