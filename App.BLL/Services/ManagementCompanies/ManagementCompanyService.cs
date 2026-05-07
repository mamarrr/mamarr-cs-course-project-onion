using System.ComponentModel.DataAnnotations;
using App.BLL.Contracts.Common;
using App.BLL.Contracts.ManagementCompanies;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.ManagementCompanies;
using App.BLL.DTO.ManagementCompanies.Models;
using App.BLL.Mappers.ManagementCompanies;
using App.BLL.Shared.Routing;
using App.DAL.Contracts;
using App.DAL.Contracts.Repositories;
using App.DAL.DTO.ManagementCompanies;
using Base.BLL;
using FluentResults;

namespace App.BLL.Services.ManagementCompanies;

public class ManagementCompanyService :
    BaseService<ManagementCompanyBllDto, ManagementCompanyDalDto, IManagementCompanyRepository, IAppUOW>,
    IManagementCompanyService
{
    private const string InitialManagementRoleCode = "OWNER";

    private readonly IAppUOW _uow;
    private readonly ICompanyMembershipService _membershipService;

    public ManagementCompanyService(
        IAppUOW uow,
        ICompanyMembershipService membershipService)
        : base(uow.ManagementCompanies, uow, new ManagementCompanyBllDtoMapper())
    {
        _uow = uow;
        _membershipService = membershipService;
    }

    public async Task<Result<ManagementCompanyBllDto>> CreateAsync(
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

        var registryCode = dto.RegistryCode.Trim();
        var registryCodeExists = await _uow.ManagementCompanies.RegistryCodeExistsAsync(registryCode, cancellationToken);
        if (registryCodeExists)
        {
            return Result.Fail(new ConflictError("Management company with the same registry code already exists."));
        }

        var initialRole = await _uow.Lookups.FindManagementCompanyRoleByCodeAsync(
            InitialManagementRoleCode,
            cancellationToken);
        if (initialRole is null)
        {
            return Result.Fail(new BusinessRuleError($"Initial management role '{InitialManagementRoleCode}' was not found."));
        }

        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var companyName = dto.Name.Trim();
            var slugs = await _uow.ManagementCompanies.AllSlugsAsync(cancellationToken);

            dto.Id = Guid.NewGuid();
            dto.Name = companyName;
            dto.Slug = SlugGenerator.EnsureUniqueSlug(SlugGenerator.GenerateSlug(companyName), slugs);
            dto.RegistryCode = registryCode;
            dto.VatNumber = dto.VatNumber.Trim();
            dto.Email = dto.Email.Trim();
            dto.Phone = dto.Phone.Trim();
            dto.Address = dto.Address.Trim();

            var addResult = AddCore(dto);
            if (addResult.IsFailed)
            {
                await _uow.RollbackTransactionAsync(cancellationToken);
                return Result.Fail<ManagementCompanyBllDto>(addResult.Errors);
            }

            _uow.ManagementCompanies.AddMembership(new ManagementCompanyMembershipCreateDalDto
            {
                Id = Guid.NewGuid(),
                ManagementCompanyId = dto.Id,
                AppUserId = appUserId,
                RoleId = initialRole.Id,
                JobTitle = "Owner",
                ValidFrom = DateOnly.FromDateTime(now),
            });

            await _uow.SaveChangesAsync(cancellationToken);
            await _uow.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
            return Result.Fail(new ConflictError("Failed to create management company due to data conflict."));
        }

        return await FindAsync(dto.Id, default, cancellationToken);
    }

    public Task<Result<CompanyMembershipContext>> AuthorizeManagementAreaAccessAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default)
    {
        return _membershipService.AuthorizeManagementAreaAccessAsync(route, cancellationToken);
    }

    public async Task<Result<CompanyProfileModel>> GetProfileAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default)
    {
        var auth = await _membershipService.AuthorizeManagementAreaAccessAsync(route, cancellationToken);

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

    public async Task<Result<ManagementCompanyBllDto>> UpdateAsync(
        ManagementCompanyRoute route,
        ManagementCompanyBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var auth = await _membershipService.AuthorizeManagementAreaAccessAsync(route, cancellationToken);

        if (auth.IsFailed)
        {
            return Result.Fail(auth.Errors);
        }

        var validationError = ValidateRequiredCompanyFields(dto);
        if (validationError is not null)
        {
            return Result.Fail<ManagementCompanyBllDto>(validationError);
        }

        var normalizedEmail = dto.Email.Trim();
        var emailAttribute = new EmailAddressAttribute();
        if (!emailAttribute.IsValid(normalizedEmail))
        {
            return Result.Fail<ManagementCompanyBllDto>(ValidationError(App.Resources.Views.UiText.InvalidEmailAddress, nameof(dto.Email)));
        }

        var normalizedRegistryCode = dto.RegistryCode.Trim();
        var duplicateRegistryCode = await _uow.ManagementCompanies.RegistryCodeExistsOutsideCompanyAsync(
            auth.Value.ManagementCompanyId,
            normalizedRegistryCode,
            cancellationToken);

        if (duplicateRegistryCode)
        {
            return Result.Fail<ManagementCompanyBllDto>(ValidationError(
                App.Resources.Views.UiText.ResourceManager.GetString("CustomerRegistryCodeAlreadyExists")
                ?? "Registry code already exists.",
                nameof(dto.RegistryCode)));
        }

        var profile = await _uow.ManagementCompanies.FirstProfileByIdAsync(
            auth.Value.ManagementCompanyId,
            cancellationToken);
        if (profile is null)
        {
            return Result.Fail<ManagementCompanyBllDto>(new NotFoundError("Management company profile was not found."));
        }

        dto.Id = auth.Value.ManagementCompanyId;
        dto.Slug = profile.Slug;
        dto.Name = dto.Name.Trim();
        dto.RegistryCode = normalizedRegistryCode;
        dto.VatNumber = dto.VatNumber.Trim();
        dto.Email = normalizedEmail;
        dto.Phone = dto.Phone.Trim();
        dto.Address = dto.Address.Trim();

        var updated = await base.UpdateAsync(dto, default, cancellationToken);
        if (updated.IsFailed)
        {
            return Result.Fail<ManagementCompanyBllDto>(updated.Errors);
        }
        await _uow.SaveChangesAsync(cancellationToken);

        return updated;
    }

    public async Task<Result<CompanyProfileModel>> UpdateAndGetProfileAsync(
        ManagementCompanyRoute route,
        ManagementCompanyBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var updated = await UpdateAsync(route, dto, cancellationToken);
        return updated.IsFailed
            ? Result.Fail<CompanyProfileModel>(updated.Errors)
            : await GetProfileAsync(route, cancellationToken);
    }

    public async Task<Result> DeleteAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default)
    {
        var auth = await _membershipService.AuthorizeManagementAreaAccessAsync(
            route,
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
