using App.BLL.Contracts;
using App.BLL.Contracts.Onboarding.Commands;
using App.BLL.Contracts.Onboarding.Models;
using App.Domain.Identity;
using FluentResults;
using Microsoft.AspNetCore.Identity;

namespace WebApp.Services.Account;

public class WebAccountService : IWebAccountService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IAppBLL _bll;

    public WebAccountService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IAppBLL bll)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _bll = bll;
    }

    public async Task<Result<AccountRegisterModel>> RegisterAsync(
        RegisterAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(command.Email);
        if (existingUser is not null)
        {
            return Result.Fail("A user with this email already exists.");
        }

        var user = new AppUser
        {
            Email = command.Email.Trim(),
            UserName = command.Email.Trim(),
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim()
        };

        var result = await _userManager.CreateAsync(user, command.Password);
        if (!result.Succeeded)
        {
            return Result.Fail(result.Errors.Select(e => e.Description));
        }

        return Result.Ok(new AccountRegisterModel
        {
            AppUserId = user.Id,
            Email = user.Email
        });
    }

    public async Task<Result<AccountLoginModel>> LoginAsync(
        LoginAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _signInManager.PasswordSignInAsync(
            command.Email.Trim(),
            command.Password,
            command.RememberMe,
            lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            return Result.Fail("Invalid login attempt.");
        }

        var user = await _userManager.FindByEmailAsync(command.Email.Trim());
        if (user is null)
        {
            return Result.Fail("User was not found.");
        }

        return Result.Ok(new AccountLoginModel
        {
            AppUserId = user.Id,
            Email = user.Email!
        });
    }

    public async Task<Result> LogoutAsync(CancellationToken cancellationToken = default)
    {
        await _signInManager.SignOutAsync();
        return Result.Ok();
    }

    public async Task<Result<CreateManagementCompanyModel>> CreateManagementCompanyAsync(
        CreateManagementCompanyCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(command.AppUserId.ToString());
        if (user is null)
        {
            return Result.Fail("Authenticated user was not found.");
        }

        return await _bll.AccountOnboarding.CreateManagementCompanyAsync(
            command,
            cancellationToken);
    }
}