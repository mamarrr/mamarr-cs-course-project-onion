using App.Domain.Identity;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using App.BLL.DTO.Onboarding.Commands;
using App.BLL.DTO.Onboarding.Models;

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

    public async Task<Result<AccountRegisterModel>> CreateUserAsync(
        RegisterAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var appUser = new AppUser
        {
            Email = command.Email,
            UserName = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(appUser, command.Password);
        if (!createResult.Succeeded)
        {
            return Result.Fail<AccountRegisterModel>(
                createResult.Errors.Select(error => error.Description));
        }

        return Result.Ok(new AccountRegisterModel
        {
            AppUserId = appUser.Id,
            Email = appUser.Email ?? command.Email
        });
    }

    public async Task<Result<AccountLoginModel>> PasswordSignInAsync(
        LoginAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.FindByEmailAsync(command.Email);
        if (appUser == null)
        {
            return Result.Fail<AccountLoginModel>(App.Resources.Views.UiText.InvalidEmailOrPassword);
        }

        var signInResult = await _signInManager.PasswordSignInAsync(
            appUser,
            command.Password,
            command.RememberMe,
            lockoutOnFailure: true);

        return signInResult.Succeeded
            ? Result.Ok(new AccountLoginModel
            {
                AppUserId = appUser.Id,
                Email = appUser.Email ?? command.Email
            })
            : Result.Fail<AccountLoginModel>(App.Resources.Views.UiText.InvalidEmailOrPassword);
    }

    public Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        return _signInManager.SignOutAsync();
    }
}
