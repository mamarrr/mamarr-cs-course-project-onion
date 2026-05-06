using System.Globalization;
using App.BLL.Contracts.Common;
using App.BLL.Contracts.Onboarding;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.ManagementCompanies;
using App.BLL.DTO.Onboarding.Commands;
using App.BLL.DTO.Onboarding.Models;
using App.BLL.DTO.Onboarding.Queries;
using App.BLL.Shared.Routing;
using App.DAL.Contracts;
using App.DAL.DTO.ManagementCompanies;
using FluentResults;

namespace App.BLL.Services.Onboarding;

public class OnboardingService : IOnboardingService
{
    private const string InitialManagementRoleCode = "OWNER";
    
    private readonly IAppUOW _uow;

    public OnboardingService(
        IAppUOW uow)
    {
        _uow = uow;
    }

    public async Task<Result<CreateManagementCompanyModel>> CreateManagementCompanyAsync(
        Guid appUserId,
        ManagementCompanyBllDto dto,
        CancellationToken cancellationToken = default)
    {
        if (appUserId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authenticated user is required."));
        }

        var validationError = ValidateRequiredCompanyFields(dto);
        if (validationError is not null)
        {
            return Result.Fail(validationError);
        }

        return await CreateManagementCompanyCoreAsync(appUserId, dto, registryCode: dto.RegistryCode.Trim(), cancellationToken);
    }

    private async Task<Result<CreateManagementCompanyModel>> CreateManagementCompanyCoreAsync(
        Guid appUserId,
        ManagementCompanyBllDto dto,
        string registryCode,
        CancellationToken cancellationToken)
    {
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
            var companyName = dto.Name.Trim();
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
                VatNumber = dto.VatNumber.Trim(),
                Email = dto.Email.Trim(),
                Phone = dto.Phone.Trim(),
                Address = dto.Address.Trim(),
            };
            _uow.ManagementCompanies.Add(company);

            _uow.ManagementCompanies.AddMembership(new ManagementCompanyMembershipCreateDalDto
            {
                Id = Guid.NewGuid(),
                ManagementCompanyId = company.Id,
                AppUserId = appUserId,
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

    private async Task<Result<OnboardingStateModel>> GetStateAsync(
        GetOnboardingStateQuery query,
        CancellationToken cancellationToken = default)
    {
        var hasAnyContext = await HasAnyContextAsync(query.AppUserId, cancellationToken);
        var defaultSlug = await GetDefaultManagementCompanySlugAsync(query.AppUserId, cancellationToken);
        return Result.Ok(new OnboardingStateModel
        {
            HasAnyContext = hasAnyContext.Value,
            DefaultManagementCompanySlug = defaultSlug.Value
        });
    }

    public Task<Result<OnboardingStateModel>> GetStateAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        return GetStateAsync(new GetOnboardingStateQuery { AppUserId = appUserId }, cancellationToken);
    }

    public Task<Result> CompleteAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Ok());
    }

    public async Task<Result<bool>> HasAnyContextAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        var hasManagementContext = (await _uow.ManagementCompanies.ActiveUserManagementContextsAsync(
            appUserId,
            cancellationToken)).Count > 0;

        if (hasManagementContext)
        {
            return Result.Ok(true);
        }

        return Result.Ok(await _uow.Residents.HasActiveUserResidentContextAsync(appUserId, cancellationToken));
    }

    public async Task<Result<string?>> GetDefaultManagementCompanySlugAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        return Result.Ok<string?>((await _uow.ManagementCompanies.ActiveUserManagementContextsAsync(
                appUserId,
                cancellationToken))
            .Select(context => context.Slug)
            .FirstOrDefault());
    }

    public Task<Result<bool>> UserHasManagementCompanyAccessAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(route.CompanySlug))
        {
            return Task.FromResult(Result.Ok(false));
        }

        return HasManagementCompanyAccessAsync(route, cancellationToken);
    }

    public async Task<Result<OnboardingJoinRequestModel>> CreateJoinRequestAsync(
        CreateCompanyJoinRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        var registryCode = command.RegistryCode.Trim();
        if (registryCode.Length == 0)
        {
            return Result.Fail(new ValidationAppError(
                "Validation failed.",
                [
                    new ValidationFailureModel
                    {
                        PropertyName = nameof(command.RegistryCode),
                        ErrorMessage = L("ManagementCompanyWasNotFound", "Management company was not found.")
                    }
                ]));
        }

        var company = await _uow.ManagementCompanies.FirstActiveByRegistryCodeAsync(
            registryCode,
            cancellationToken);
        if (company == null)
        {
            return Result.Fail(new NotFoundError(L("ManagementCompanyWasNotFound", "Management company was not found.")));
        }

        var role = await _uow.Lookups.FindManagementCompanyRoleByIdAsync(
            command.RequestedRoleId,
            cancellationToken);
        if (role == null)
        {
            return Result.Fail(new ValidationAppError(
                "Validation failed.",
                [
                    new ValidationFailureModel
                    {
                        PropertyName = nameof(command.RequestedRoleId),
                        ErrorMessage = L("SelectedRoleIsInvalid", "Selected role is invalid.")
                    }
                ]));
        }

        var membershipExists = await _uow.ManagementCompanies.MembershipExistsAsync(
            command.AppUserId,
            company.Id,
            cancellationToken);
        if (membershipExists)
        {
            return Result.Fail(new ConflictError(L("AlreadyMemberOfThisManagementCompany", "You are already a member of this management company.")));
        }

        var pendingStatus = await _uow.Lookups.FindManagementCompanyJoinRequestStatusByCodeAsync(
            ManagementCompanyJoinRequestStatusCodes.Pending,
            cancellationToken);
        if (pendingStatus == null)
        {
            throw new InvalidOperationException(
                $"Management company join request status '{ManagementCompanyJoinRequestStatusCodes.Pending}' is not seeded.");
        }

        var duplicatePending = await _uow.ManagementCompanyJoinRequests.HasPendingRequestAsync(
            command.AppUserId,
            company.Id,
            pendingStatus.Id,
            cancellationToken);
        if (duplicatePending)
        {
            return Result.Fail(new ConflictError(L("PendingRequestForThisCompanyAlreadyExists", "A pending request for this company already exists.")));
        }

        var requestId = _uow.ManagementCompanyJoinRequests.Add(new ManagementCompanyJoinRequestDalDto
        {
            AppUserId = command.AppUserId,
            ManagementCompanyId = company.Id,
            RequestedRoleId = command.RequestedRoleId,
            StatusId = pendingStatus.Id,
            Message = string.IsNullOrWhiteSpace(command.Message) ? null : command.Message.Trim(),
        });

        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            return Result.Fail(new ConflictError(L("PendingRequestForThisCompanyAlreadyExists", "A pending request for this company already exists.")));
        }

        return Result.Ok(new OnboardingJoinRequestModel
        {
            RequestId = requestId
        });
    }

    private async Task<Result<bool>> HasManagementCompanyAccessAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken)
    {
        return Result.Ok(await _uow.ManagementCompanies.ActiveUserManagementContextExistsBySlugAsync(
            route.AppUserId,
            route.CompanySlug,
            cancellationToken));
    }

    private static string L(string resourceKey, string fallback)
    {
        return App.Resources.Views.UiText.ResourceManager.GetString(resourceKey, CultureInfo.CurrentUICulture) ?? fallback;
    }

    private static ValidationAppError? ValidateRequiredCompanyFields(ManagementCompanyBllDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return Required(App.Resources.Views.UiText.Name);
        }

        if (string.IsNullOrWhiteSpace(dto.RegistryCode))
        {
            return Required(App.Resources.Views.UiText.RegistryCode);
        }

        if (string.IsNullOrWhiteSpace(dto.VatNumber))
        {
            return Required(App.Resources.Views.UiText.VatNumber);
        }

        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            return Required(App.Resources.Views.UiText.Email);
        }

        if (string.IsNullOrWhiteSpace(dto.Phone))
        {
            return Required(App.Resources.Views.UiText.Phone);
        }

        return string.IsNullOrWhiteSpace(dto.Address)
            ? Required(App.Resources.Views.UiText.Address)
            : null;
    }

    private static ValidationAppError Required(string fieldName)
    {
        return new ValidationAppError(
            App.Resources.Views.UiText.RequiredField.Replace("{0}", fieldName),
            [
                new ValidationFailureModel
                {
                    PropertyName = fieldName,
                    ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", fieldName)
                }
            ]);
    }
}
