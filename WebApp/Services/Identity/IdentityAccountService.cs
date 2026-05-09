using App.Domain.Identity;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace WebApp.Services.Identity;

public class IdentityAccountService : IIdentityAccountService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public IdentityAccountService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<Guid?> GetAuthenticatedUserIdAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.GetUserAsync(principal);
        return appUser?.Id;
    }

    public async Task<Guid?> FindUserIdByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.FindByEmailAsync(email);
        return appUser?.Id;
    }

    public async Task<bool> UserExistsAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        return await _userManager.FindByIdAsync(appUserId.ToString()) != null;
    }

    public async Task<bool> IsInRoleAsync(
        Guid appUserId,
        string role,
        CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.FindByIdAsync(appUserId.ToString());
        return appUser != null && await _userManager.IsInRoleAsync(appUser, role);
    }

    public async Task<Result> CreateUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default)
    {
        var appUser = new AppUser
        {
            Email = email,
            UserName = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(appUser, password);
        if (!createResult.Succeeded)
        {
            return Result.Fail(createResult.Errors.Select(error => error.Description));
        }

        return Result.Ok();
    }

    public async Task<Result<Guid>> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.FindByEmailAsync(email);
        if (appUser == null)
        {
            return Result.Fail<Guid>(App.Resources.Views.UiText.InvalidEmailOrPassword);
        }

        var isValidPassword = await _userManager.CheckPasswordAsync(appUser, password);
        return isValidPassword
            ? Result.Ok(appUser.Id)
            : Result.Fail<Guid>(App.Resources.Views.UiText.InvalidEmailOrPassword);
    }

    public async Task<Result<IdentityUserInfo>> GetUserInfoAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.FindByIdAsync(appUserId.ToString());
        if (appUser == null)
        {
            return Result.Fail<IdentityUserInfo>("User was not found.");
        }

        var roles = await _userManager.GetRolesAsync(appUser);

        return Result.Ok(new IdentityUserInfo
        {
            Id = appUser.Id,
            Email = appUser.Email ?? string.Empty,
            FirstName = appUser.FirstName,
            LastName = appUser.LastName,
            Roles = roles.ToList()
        });
    }

    public async Task<Result<Guid>> PasswordSignInAsync(
        string email,
        string password,
        bool rememberMe,
        CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.FindByEmailAsync(email);
        if (appUser == null)
        {
            return Result.Fail<Guid>(App.Resources.Views.UiText.InvalidEmailOrPassword);
        }

        var signInResult = await _signInManager.PasswordSignInAsync(
            appUser,
            password,
            rememberMe,
            lockoutOnFailure: true);

        return signInResult.Succeeded
            ? Result.Ok(appUser.Id)
            : Result.Fail<Guid>(App.Resources.Views.UiText.InvalidEmailOrPassword);
    }

    public Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        return _signInManager.SignOutAsync();
    }
}
