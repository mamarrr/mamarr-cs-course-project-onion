using System.ComponentModel.DataAnnotations;
using App.BLL.Contracts.Common;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.ManagementCompanies;
using App.BLL.Contracts.ManagementCompanies.Models;
using App.DAL.Contracts;
using App.DAL.DTO.ManagementCompanies;
using FluentResults;

namespace App.BLL.Services.ManagementCompanies;

public class ManagementCompanyProfileService : IManagementCompanyProfileService
{
    private readonly IAppUOW _uow;
    private readonly ICompanyMembershipAdminService _membershipService;

    public ManagementCompanyProfileService(
        IAppUOW uow,
        ICompanyMembershipAdminService membershipService)
    {
        _uow = uow;
        _membershipService = membershipService;
    }

    public async Task<Result<CompanyProfileModel>> GetProfileAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var auth = await _membershipService.AuthorizeManagementAreaAccessAsync(
            appUserId,
            companySlug,
            cancellationToken);

        if (auth.IsFailed)
        {
            return Result.Fail(auth.Errors);
        }

        var profile = await _uow.ManagementCompanies.FirstProfileByIdAsync(
            auth.Value.ManagementCompanyId,
            cancellationToken);

        return profile is null
            ? Result.Fail<CompanyProfileModel>(new NotFoundError("Management company profile was not found."))
            : Result.Ok(MapProfile(profile));
    }

    public async Task<Result> UpdateProfileAsync(
        Guid appUserId,
        string companySlug,
        CompanyProfileUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var auth = await _membershipService.AuthorizeManagementAreaAccessAsync(
            appUserId,
            companySlug,
            cancellationToken);

        if (auth.IsFailed)
        {
            return Result.Fail(auth.Errors);
        }

        var validationError = ValidateRequiredCompanyFields(request);
        if (validationError is not null)
        {
            return Result.Fail(validationError);
        }

        var normalizedEmail = request.Email.Trim();
        var emailAttribute = new EmailAddressAttribute();
        if (!emailAttribute.IsValid(normalizedEmail))
        {
            return Result.Fail(ValidationError(App.Resources.Views.UiText.InvalidEmailAddress, nameof(request.Email)));
        }

        var normalizedRegistryCode = request.RegistryCode.Trim();
        var duplicateRegistryCode = await _uow.ManagementCompanies.RegistryCodeExistsOutsideCompanyAsync(
            auth.Value.ManagementCompanyId,
            normalizedRegistryCode,
            cancellationToken);

        if (duplicateRegistryCode)
        {
            return Result.Fail(ValidationError(
                App.Resources.Views.UiText.ResourceManager.GetString("CustomerRegistryCodeAlreadyExists")
                ?? "Registry code already exists.",
                nameof(request.RegistryCode)));
        }

        var updated = await _uow.ManagementCompanies.UpdateProfileAsync(
            new ManagementCompanyProfileUpdateDalDto
            {
                Id = auth.Value.ManagementCompanyId,
                Name = request.Name.Trim(),
                RegistryCode = normalizedRegistryCode,
                VatNumber = request.VatNumber.Trim(),
                Email = normalizedEmail,
                Phone = request.Phone.Trim(),
                Address = request.Address.Trim(),
            },
            cancellationToken);

        if (!updated)
        {
            return Result.Fail(new NotFoundError("Management company profile was not found."));
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> DeleteProfileAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var auth = await _membershipService.AuthorizeManagementAreaAccessAsync(
            appUserId,
            companySlug,
            cancellationToken);

        if (auth.IsFailed)
        {
            return Result.Fail(auth.Errors);
        }

        if (!IsOwnerOrManager(auth.Value.ActorRoleCode))
        {
            return Result.Fail(new ForbiddenError(App.Resources.Views.UiText.AccessDeniedDescription));
        }

        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var deleted = await _uow.ManagementCompanies.DeleteCascadeAsync(
                auth.Value.ManagementCompanyId,
                cancellationToken);

            if (!deleted)
            {
                await _uow.RollbackTransactionAsync(cancellationToken);
                return Result.Fail(new NotFoundError("Management company profile was not found."));
            }

            await _uow.CommitTransactionAsync(cancellationToken);
            return Result.Ok();
        }
        catch
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static CompanyProfileModel MapProfile(ManagementCompanyProfileDalDto profile)
    {
        return new CompanyProfileModel
        {
            ManagementCompanyId = profile.Id,
            CompanySlug = profile.Slug,
            Name = profile.Name,
            RegistryCode = profile.RegistryCode,
            VatNumber = profile.VatNumber,
            Email = profile.Email,
            Phone = profile.Phone,
            Address = profile.Address,
        };
    }

    private static bool IsOwnerOrManager(string roleCode)
    {
        return string.Equals(roleCode, "OWNER", StringComparison.OrdinalIgnoreCase)
               || string.Equals(roleCode, "MANAGER", StringComparison.OrdinalIgnoreCase);
    }

    private static ValidationAppError? ValidateRequiredCompanyFields(CompanyProfileUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Required(App.Resources.Views.UiText.Name);
        }

        if (string.IsNullOrWhiteSpace(request.RegistryCode))
        {
            return Required(App.Resources.Views.UiText.RegistryCode);
        }

        if (string.IsNullOrWhiteSpace(request.VatNumber))
        {
            return Required(App.Resources.Views.UiText.VatNumber);
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Required(App.Resources.Views.UiText.Email);
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return Required(App.Resources.Views.UiText.Phone);
        }

        return string.IsNullOrWhiteSpace(request.Address)
            ? Required(App.Resources.Views.UiText.Address)
            : null;
    }

    private static ValidationAppError Required(string fieldName)
    {
        return ValidationError(App.Resources.Views.UiText.RequiredField.Replace("{0}", fieldName), fieldName);
    }

    private static ValidationAppError ValidationError(string message, string propertyName)
    {
        return new ValidationAppError(
            message,
            [
                new ValidationFailureModel
                {
                    PropertyName = propertyName,
                    ErrorMessage = message
                }
            ]);
    }
}
