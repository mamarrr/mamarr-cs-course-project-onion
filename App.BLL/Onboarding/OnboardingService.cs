using App.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace App.BLL.Onboarding;

public class OnboardingService : IOnboardingService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public OnboardingService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<OnboardingRegisterResult> RegisterAsync(OnboardingRegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return new OnboardingRegisterResult
            {
                Succeeded = false,
                Errors = new[] { "A user with this email already exists." }
            };
        }

        var appUser = new AppUser
        {
            Email = request.Email,
            UserName = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(appUser, request.Password);

        return new OnboardingRegisterResult
        {
            Succeeded = createResult.Succeeded,
            Errors = createResult.Errors.Select(e => e.Description).ToArray()
        };
    }

    public async Task<OnboardingLoginResult> LoginAsync(OnboardingLoginRequest request)
    {
        var appUser = await _userManager.FindByEmailAsync(request.Email);
        if (appUser == null)
        {
            return new OnboardingLoginResult { Succeeded = false };
        }

        var signInResult = await _signInManager.PasswordSignInAsync(
            appUser,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true
        );

        return new OnboardingLoginResult
        {
            Succeeded = signInResult.Succeeded
        };
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }
}

