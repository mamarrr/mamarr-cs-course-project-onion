using App.BLL.Contracts.Onboarding.Commands;
using App.BLL.Contracts.Onboarding.Models;
using App.BLL.Contracts.Onboarding.Queries;
using App.BLL.Contracts.Onboarding.Services;
using App.BLL.Shared.Routing;
using App.Contracts;
using App.Contracts.DAL.ManagementCompanies;
using FluentResults;

namespace App.BLL.Onboarding.Account;

public sealed class AccountOnboardingService : IAccountOnboardingService
{
    private const string InitialManagementRoleCode = "OWNER";

    private readonly IAccountIdentityService _identityService;
    private readonly IAppUOW _uow;

    public AccountOnboardingService(
        IAccountIdentityService identityService,
        IAppUOW uow)
    {
        _identityService = identityService;
        _uow = uow;
    }

    public async Task<Result<AccountRegisterModel>> RegisterAsync(
        RegisterAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var existingUserId = await _identityService.FindUserIdByEmailAsync(
            command.Email,
            cancellationToken);
        if (existingUserId.HasValue)
        {
            return Result.Fail("A user with this email already exists.");
        }

        return await _identityService.CreateUserAsync(command, cancellationToken);
    }

    public async Task<Result<AccountLoginModel>> LoginAsync(
        LoginAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        return await _identityService.PasswordSignInAsync(command, cancellationToken);
    }

    public async Task<Result> LogoutAsync(
        LogoutCommand command,
        CancellationToken cancellationToken = default)
    {
        await _identityService.SignOutAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result<CreateManagementCompanyModel>> CreateManagementCompanyAsync(
        CreateManagementCompanyCommand command,
        CancellationToken cancellationToken = default)
    {
        var userExists = await _identityService.UserExistsAsync(command.AppUserId, cancellationToken);
        if (!userExists)
        {
            return Result.Fail("Authenticated user was not found.");
        }

        var registryCode = command.RegistryCode.Trim();
        var registryCodeExists = await _uow.ManagementCompanies.RegistryCodeExistsAsync(
            registryCode,
            cancellationToken);
        if (registryCodeExists)
        {
            return Result.Fail("Management company with the same registry code already exists.");
        }

        var initialRole = await _uow.Lookups.FindManagementCompanyRoleByCodeAsync(
            InitialManagementRoleCode,
            cancellationToken);
        if (initialRole == null)
        {
            return Result.Fail($"Initial management role '{InitialManagementRoleCode}' was not found.");
        }

        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var companyName = command.Name.Trim();
            var companySlug = SlugGenerator.EnsureUniqueSlug(
                companyName,
                await _uow.ManagementCompanies.AllSlugsAsync(cancellationToken));

            var companyId = Guid.NewGuid();
            var company = await _uow.ManagementCompanies.AddManagementCompanyAsync(
                new ManagementCompanyCreateDalDto
                {
                    Id = companyId,
                    Name = companyName,
                    Slug = companySlug,
                    RegistryCode = registryCode,
                    VatNumber = command.VatNumber.Trim(),
                    Email = command.Email.Trim(),
                    Phone = command.Phone.Trim(),
                    Address = command.Address.Trim(),
                    CreatedAt = now,
                    IsActive = true
                },
                cancellationToken);

            _uow.ManagementCompanies.AddMembership(new ManagementCompanyMembershipCreateDalDto
            {
                Id = Guid.NewGuid(),
                ManagementCompanyId = company.Id,
                AppUserId = command.AppUserId,
                RoleId = initialRole.Id,
                JobTitle = "Owner",
                ValidFrom = DateOnly.FromDateTime(now),
                IsActive = true,
                CreatedAt = now
            });

            await _uow.SaveChangesAsync(cancellationToken);
            await _uow.CommitTransactionAsync(cancellationToken);

            return Result.Ok(new CreateManagementCompanyModel
            {
                ManagementCompanyId = company.Id,
                ManagementCompanySlug = company.Slug,
                Name = company.Name
            });
        }
        catch
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
            return Result.Fail("Failed to create management company due to data conflict.");
        }
    }

    public async Task<Result<OnboardingStateModel>> GetStateAsync(
        GetOnboardingStateQuery query,
        CancellationToken cancellationToken = default)
    {
        return Result.Ok(new OnboardingStateModel
        {
            HasAnyContext = await HasAnyContextAsync(query.AppUserId, cancellationToken),
            DefaultManagementCompanySlug = await GetDefaultManagementCompanySlugAsync(query.AppUserId, cancellationToken)
        });
    }

    public Task<Result> CompleteAsync(
        CompleteAccountOnboardingCommand command,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Ok());
    }

    public async Task<bool> HasAnyContextAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        var hasManagementContext = (await _uow.ManagementCompanies.ActiveUserManagementContextsAsync(
            appUserId,
            cancellationToken)).Count > 0;

        if (hasManagementContext)
        {
            return true;
        }

        return await _uow.Residents.HasActiveUserResidentContextAsync(appUserId, cancellationToken);
    }

    public async Task<string?> GetDefaultManagementCompanySlugAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        return (await _uow.ManagementCompanies.ActiveUserManagementContextsAsync(
                appUserId,
                cancellationToken))
            .Select(context => context.Slug)
            .FirstOrDefault();
    }

    public Task<bool> UserHasManagementCompanyAccessAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(companySlug))
        {
            return Task.FromResult(false);
        }

        return _uow.ManagementCompanies.ActiveUserManagementContextExistsBySlugAsync(
            appUserId,
            companySlug,
            cancellationToken);
    }
}
