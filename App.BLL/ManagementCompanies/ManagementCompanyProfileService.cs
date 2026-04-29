using System.ComponentModel.DataAnnotations;
using App.BLL.Contracts.ManagementCompanies.Models;
using App.BLL.Contracts.ManagementCompanies.Services;
using App.Contracts;
using App.Contracts.DAL.ManagementCompanies;

namespace App.BLL.ManagementCompanies;

public sealed class ManagementCompanyProfileService : IManagementCompanyProfileService
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

    public async Task<CompanyProfileModel?> GetProfileAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var auth = await _membershipService.AuthorizeManagementAreaAccessAsync(
            appUserId,
            companySlug,
            cancellationToken);

        if (!auth.IsAuthorized || auth.Context is null)
        {
            return null;
        }

        var profile = await _uow.ManagementCompanies.FirstProfileByIdAsync(
            auth.Context.ManagementCompanyId,
            cancellationToken);

        return profile is null ? null : MapProfile(profile);
    }

    public async Task<ProfileOperationResult> UpdateProfileAsync(
        Guid appUserId,
        string companySlug,
        CompanyProfileUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var auth = await _membershipService.AuthorizeManagementAreaAccessAsync(
            appUserId,
            companySlug,
            cancellationToken);

        if (auth.CompanyNotFound)
        {
            return new ProfileOperationResult { NotFound = true };
        }

        if (!auth.IsAuthorized || auth.Context is null)
        {
            return new ProfileOperationResult { Forbidden = true };
        }

        var validationError = ValidateRequiredCompanyFields(request);
        if (validationError is not null)
        {
            return validationError;
        }

        var normalizedEmail = request.Email.Trim();
        var emailAttribute = new EmailAddressAttribute();
        if (!emailAttribute.IsValid(normalizedEmail))
        {
            return new ProfileOperationResult
            {
                ErrorMessage = App.Resources.Views.UiText.InvalidEmailAddress
            };
        }

        var normalizedRegistryCode = request.RegistryCode.Trim();
        var duplicateRegistryCode = await _uow.ManagementCompanies.RegistryCodeExistsOutsideCompanyAsync(
            auth.Context.ManagementCompanyId,
            normalizedRegistryCode,
            cancellationToken);

        if (duplicateRegistryCode)
        {
            return new ProfileOperationResult
            {
                DuplicateRegistryCode = true,
                ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("CustomerRegistryCodeAlreadyExists")
                               ?? "Registry code already exists."
            };
        }

        var updated = await _uow.ManagementCompanies.UpdateProfileAsync(
            new ManagementCompanyProfileUpdateDalDto
            {
                Id = auth.Context.ManagementCompanyId,
                Name = request.Name.Trim(),
                RegistryCode = normalizedRegistryCode,
                VatNumber = request.VatNumber.Trim(),
                Email = normalizedEmail,
                Phone = request.Phone.Trim(),
                Address = request.Address.Trim(),
                IsActive = request.IsActive
            },
            cancellationToken);

        if (!updated)
        {
            return new ProfileOperationResult { NotFound = true };
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return new ProfileOperationResult { Success = true };
    }

    public async Task<ProfileOperationResult> DeleteProfileAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var auth = await _membershipService.AuthorizeManagementAreaAccessAsync(
            appUserId,
            companySlug,
            cancellationToken);

        if (auth.CompanyNotFound)
        {
            return new ProfileOperationResult { NotFound = true };
        }

        if (!auth.IsAuthorized || auth.Context is null)
        {
            return new ProfileOperationResult { Forbidden = true };
        }

        if (!IsOwnerOrManager(auth.Context.ActorRoleCode))
        {
            return new ProfileOperationResult
            {
                Forbidden = true,
                ErrorMessage = App.Resources.Views.UiText.AccessDeniedDescription
            };
        }

        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var deleted = await _uow.ManagementCompanies.DeleteCascadeAsync(
                auth.Context.ManagementCompanyId,
                cancellationToken);

            if (!deleted)
            {
                await _uow.RollbackTransactionAsync(cancellationToken);
                return new ProfileOperationResult { NotFound = true };
            }

            await _uow.CommitTransactionAsync(cancellationToken);
            return new ProfileOperationResult { Success = true };
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
            IsActive = profile.IsActive
        };
    }

    private static bool IsOwnerOrManager(string roleCode)
    {
        return string.Equals(roleCode, "OWNER", StringComparison.OrdinalIgnoreCase)
               || string.Equals(roleCode, "MANAGER", StringComparison.OrdinalIgnoreCase);
    }

    private static ProfileOperationResult? ValidateRequiredCompanyFields(CompanyProfileUpdateRequest request)
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

    private static ProfileOperationResult Required(string fieldName)
    {
        return new ProfileOperationResult
        {
            ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", fieldName)
        };
    }
}
