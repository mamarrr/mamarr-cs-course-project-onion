using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Onboarding;
using App.BLL.Contracts.Onboarding.Commands;
using App.BLL.Contracts.Onboarding.Models;
using App.BLL.Contracts.Onboarding.Queries;
using App.BLL.Shared.Routing;
using App.DAL.Contracts;
using App.DAL.DTO.ManagementCompanies;
using FluentResults;

namespace App.BLL.Services.Onboarding.Account;

public class AccountOnboardingService : IAccountOnboardingService
{
    private const string InitialManagementRoleCode = "OWNER";
    
    private readonly IAppUOW _uow;

    public AccountOnboardingService(
        IAppUOW uow)
    {
        _uow = uow;
    }

    public async Task<Result<CreateManagementCompanyModel>> CreateManagementCompanyAsync(
        CreateManagementCompanyCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.AppUserId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authenticated user is required."));
        }

        var registryCode = command.RegistryCode.Trim();
        var registryCodeExists = await _uow.ManagementCompanies.RegistryCodeExistsAsync(
            registryCode,
            cancellationToken);
        if (registryCodeExists)
        {
            return Result.Fail(new ConflictError("Management company with the same registry code already exists."));
        }

        var initialRole = await _uow.Lookups.FindManagementCompanyRoleByCodeAsync(
            InitialManagementRoleCode,
            cancellationToken);
        if (initialRole == null)
        {
            return Result.Fail(new BusinessRuleError($"Initial management role '{InitialManagementRoleCode}' was not found."));
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
            var company = new ManagementCompanyDalDto
            {
                Id = companyId,
                Name = companyName,
                Slug = companySlug,
                RegistryCode = registryCode,
                VatNumber = command.VatNumber.Trim(),
                Email = command.Email.Trim(),
                Phone = command.Phone.Trim(),
                Address = command.Address.Trim(),
            };
            _uow.ManagementCompanies.Add(company);

            _uow.ManagementCompanies.AddMembership(new ManagementCompanyMembershipCreateDalDto
            {
                Id = Guid.NewGuid(),
                ManagementCompanyId = company.Id,
                AppUserId = command.AppUserId,
                RoleId = initialRole.Id,
                JobTitle = "Owner",
                ValidFrom = DateOnly.FromDateTime(now),
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
            return Result.Fail(new ConflictError("Failed to create management company due to data conflict."));
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
