using App.Domain.Identity;
using App.DAL.EF;
using App.DAL.EF.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace App.BLL.Onboarding;

public class OnboardingService : IOnboardingService
{
    private const string InitialManagementRoleCode = "OWNER";

    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly AppDbContext? _dbContext;

    public OnboardingService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        AppDbContext? dbContext = null)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
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

    public async Task<bool> HasAnyContextAsync(Guid appUserId)
    {
        if (_dbContext == null) return false;

        var hasManagementContext = await _dbContext.ManagementCompanyUsers
            .AnyAsync(x => x.AppUserId == appUserId && x.IsActive);

        if (hasManagementContext) return true;

        var hasResidentContext = await _dbContext.ResidentUsers
            .AnyAsync(x => x.AppUserId == appUserId && x.IsActive);

        return hasResidentContext;
    }

    public async Task<OnboardingCreateManagementCompanyResult> CreateManagementCompanyAsync(
        OnboardingCreateManagementCompanyRequest request)
    {
        if (_dbContext == null)
        {
            return new OnboardingCreateManagementCompanyResult
            {
                Succeeded = false,
                Errors = ["Onboarding service is not configured for data operations."]
            };
        }

        var appUserExists = await _userManager.Users.AnyAsync(x => x.Id == request.AppUserId);
        if (!appUserExists)
        {
            return new OnboardingCreateManagementCompanyResult
            {
                Succeeded = false,
                Errors = ["Authenticated user was not found."]
            };
        }

        var registryCode = request.RegistryCode.Trim();
        var registryCodeExists = await _dbContext.ManagementCompanies
            .AnyAsync(x => x.RegistryCode == registryCode);

        if (registryCodeExists)
        {
            return new OnboardingCreateManagementCompanyResult
            {
                Succeeded = false,
                Errors = ["Management company with the same registry code already exists."]
            };
        }

        var initialRoleCode = InitialData.ManagementCompanyRoleSeeds
            .Select(x => x.code)
            .FirstOrDefault(x => x == InitialManagementRoleCode) ?? InitialManagementRoleCode;

        var initialRole = await _dbContext.ManagementCompanyRoles
            .SingleOrDefaultAsync(x => x.Code == initialRoleCode);

        if (initialRole == null)
        {
            return new OnboardingCreateManagementCompanyResult
            {
                Succeeded = false,
                Errors = [$"Initial management role '{initialRoleCode}' was not found."]
            };
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            var company = new App.Domain.ManagementCompany
            {
                Name = request.Name.Trim(),
                RegistryCode = registryCode,
                VatNumber = request.VatNumber.Trim(),
                Email = request.Email.Trim(),
                Phone = request.Phone.Trim(),
                Address = request.Address.Trim(),
                CreatedAt = now,
                IsActive = true
            };

            _dbContext.ManagementCompanies.Add(company);
            await _dbContext.SaveChangesAsync();

            var managementCompanyUser = new App.Domain.ManagementCompanyUser
            {
                ManagementCompanyId = company.Id,
                AppUserId = request.AppUserId,
                ManagementCompanyRoleId = initialRole.Id,
                JobTitle = "Owner",
                ValidFrom = DateOnly.FromDateTime(now),
                IsActive = true,
                CreatedAt = now
            };

            _dbContext.ManagementCompanyUsers.Add(managementCompanyUser);
            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            return new OnboardingCreateManagementCompanyResult { Succeeded = true };
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return new OnboardingCreateManagementCompanyResult
            {
                Succeeded = false,
                Errors = ["Failed to create management company due to data conflict."]
            };
        }
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }
}

