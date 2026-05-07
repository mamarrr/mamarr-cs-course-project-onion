using App.BLL.Contracts.Vendors;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Vendors;
using App.BLL.DTO.Vendors.Models;
using App.BLL.Mappers.Vendors;
using App.DAL.Contracts;
using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Tickets;
using App.DAL.DTO.Vendors;
using Base.BLL;
using FluentResults;

namespace App.BLL.Services.Vendors;

public class VendorService :
    BaseService<VendorBllDto, VendorDalDto, IVendorRepository, IAppUOW>,
    IVendorService
{
    private static readonly HashSet<string> ReadAllowedRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER",
        "FINANCE",
        "SUPPORT"
    };

    private static readonly HashSet<string> WriteAllowedRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER",
        "SUPPORT"
    };

    private static readonly HashSet<string> DeleteAllowedRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER"
    };

    private const int CategoryNotesMaxLength = 4000;

    public VendorService(IAppUOW uow)
        : base(uow.Vendors, uow, new VendorBllDtoMapper())
    {
    }

    public async Task<Result<VendorWorkspaceModel>> ResolveCompanyWorkspaceAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default)
    {
        return await ResolveCompanyAccessAsync(route, ReadAllowedRoleCodes, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<VendorListItemModel>>> ListForCompanyAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveCompanyAccessAsync(route, ReadAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<IReadOnlyList<VendorListItemModel>>(access.Errors);
        }

        var vendors = await ServiceUOW.Vendors.AllByCompanyAsync(
            access.Value.ManagementCompanyId,
            cancellationToken);

        return Result.Ok((IReadOnlyList<VendorListItemModel>)vendors
            .Select(vendor => new VendorListItemModel
            {
                VendorId = vendor.Id,
                ManagementCompanyId = vendor.ManagementCompanyId,
                CompanySlug = access.Value.CompanySlug,
                CompanyName = access.Value.CompanyName,
                Name = vendor.Name,
                RegistryCode = vendor.RegistryCode,
                CreatedAt = vendor.CreatedAt,
                ActiveCategoryCount = vendor.ActiveCategoryCount,
                AssignedTicketCount = vendor.AssignedTicketCount,
                ContactCount = vendor.ContactCount
            })
            .ToList());
    }

    public async Task<Result<VendorProfileModel>> GetProfileAsync(
        VendorRoute route,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveVendorAccessAsync(route, ReadAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<VendorProfileModel>(access.Errors);
        }

        var profile = await ServiceUOW.Vendors.FindProfileAsync(
            access.Value.VendorId,
            access.Value.ManagementCompanyId,
            cancellationToken);

        return profile is null
            ? Result.Fail<VendorProfileModel>(new NotFoundError(T("VendorWasNotFound", "Vendor was not found.")))
            : Result.Ok(ToProfileModel(profile));
    }

    public async Task<Result<VendorBllDto>> CreateAsync(
        ManagementCompanyRoute route,
        VendorBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveCompanyAccessAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<VendorBllDto>(access.Errors);
        }

        var validation = Validate(dto);
        if (validation.IsFailed)
        {
            return Result.Fail<VendorBllDto>(validation.Errors);
        }

        var normalized = Normalize(dto);
        var duplicateRegistryCode = await ServiceUOW.Vendors.RegistryCodeExistsInCompanyAsync(
            access.Value.ManagementCompanyId,
            normalized.RegistryCode,
            cancellationToken: cancellationToken);
        if (duplicateRegistryCode)
        {
            return Result.Fail<VendorBllDto>(new ConflictError(T(
                "VendorRegistryCodeAlreadyExists",
                "Vendor with this registry code already exists in this company.")));
        }

        var res = new VendorBllDto()
        {
            Id = Guid.Empty,
            ManagementCompanyId = access.Value.ManagementCompanyId,
            Name = normalized.Name,
            RegistryCode = normalized.RegistryCode,
            Notes = normalized.Notes,
        };
        

        return await AddAndFindCoreAsync(res, access.Value.ManagementCompanyId, cancellationToken);
    }

    public async Task<Result<VendorProfileModel>> CreateAndGetProfileAsync(
        ManagementCompanyRoute route,
        VendorBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var created = await CreateAsync(route, dto, cancellationToken);
        if (created.IsFailed)
        {
            return Result.Fail<VendorProfileModel>(created.Errors);
        }

        return await GetProfileAsync(
            new VendorRoute
            {
                AppUserId = route.AppUserId,
                CompanySlug = route.CompanySlug,
                VendorId = created.Value.Id
            },
            cancellationToken);
    }

    public async Task<Result<VendorBllDto>> UpdateAsync(
        VendorRoute route,
        VendorBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveVendorAccessAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<VendorBllDto>(access.Errors);
        }

        var validation = Validate(dto);
        if (validation.IsFailed)
        {
            return Result.Fail<VendorBllDto>(validation.Errors);
        }

        var normalized = Normalize(dto);
        var duplicateRegistryCode = await ServiceUOW.Vendors.RegistryCodeExistsInCompanyAsync(
            access.Value.ManagementCompanyId,
            normalized.RegistryCode,
            access.Value.VendorId,
            cancellationToken);
        if (duplicateRegistryCode)
        {
            return Result.Fail<VendorBllDto>(new ConflictError(T(
                "VendorRegistryCodeAlreadyExists",
                "Vendor with this registry code already exists in this company.")));
        }

        dto.Id = access.Value.VendorId;
        dto.ManagementCompanyId = access.Value.ManagementCompanyId;
        dto.Name = normalized.Name;
        dto.RegistryCode = normalized.RegistryCode;
        dto.Notes = normalized.Notes;

        var updated = await base.UpdateAsync(dto, access.Value.ManagementCompanyId, cancellationToken);
        if (updated.IsFailed)
        {
            return Result.Fail<VendorBllDto>(updated.Errors);
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return updated;
    }

    public async Task<Result<VendorProfileModel>> UpdateAndGetProfileAsync(
        VendorRoute route,
        VendorBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var updated = await UpdateAsync(route, dto, cancellationToken);
        return updated.IsFailed
            ? Result.Fail<VendorProfileModel>(updated.Errors)
            : await GetProfileAsync(route, cancellationToken);
    }

    public async Task<Result> DeleteAsync(
        VendorRoute route,
        string confirmationRegistryCode,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveVendorAccessAsync(route, DeleteAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail(access.Errors);
        }

        var profile = await ServiceUOW.Vendors.FindProfileAsync(
            access.Value.VendorId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (profile is null)
        {
            return Result.Fail(new NotFoundError(T("VendorWasNotFound", "Vendor was not found.")));
        }

        if (!string.Equals(confirmationRegistryCode?.Trim(), profile.RegistryCode.Trim(), StringComparison.Ordinal))
        {
            var message = T(
                "VendorDeleteConfirmationMismatch",
                "Delete confirmation does not match the current vendor registry code.");
            return Result.Fail(new ValidationAppError(
                message,
                [
                    new ValidationFailureModel
                    {
                        PropertyName = "ConfirmationRegistryCode",
                        ErrorMessage = message
                    }
                ]));
        }

        var hasDependencies = await ServiceUOW.Vendors.HasDeleteDependenciesAsync(
            access.Value.VendorId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (hasDependencies)
        {
            return Result.Fail(new BusinessRuleError(T(
                "VendorDeleteBlockedByDependencies",
                "Unable to delete vendor because tickets, scheduled work, contacts, or category assignments exist.")));
        }

        var removed = await base.RemoveAsync(
            access.Value.VendorId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (removed.IsFailed)
        {
            return Result.Fail(removed.Errors);
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result<VendorCategoryAssignmentListModel>> ListCategoryAssignmentsAsync(
        VendorRoute route,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveVendorAccessAsync(route, ReadAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<VendorCategoryAssignmentListModel>(access.Errors);
        }

        return await BuildCategoryAssignmentListAsync(route, access.Value, cancellationToken);
    }

    public async Task<Result<VendorCategoryAssignmentListModel>> AssignCategoryAsync(
        VendorRoute route,
        VendorTicketCategoryBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveVendorAccessAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<VendorCategoryAssignmentListModel>(access.Errors);
        }

        var validation = await ValidateCategoryAssignmentAsync(dto, cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<VendorCategoryAssignmentListModel>(validation.Errors);
        }

        var duplicate = await ServiceUOW.VendorTicketCategories.ExistsInCompanyAsync(
            access.Value.VendorId,
            dto.TicketCategoryId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (duplicate)
        {
            return Result.Fail<VendorCategoryAssignmentListModel>(new ConflictError(T(
                "VendorCategoryAlreadyAssigned",
                "This category is already assigned to the vendor.")));
        }

        ServiceUOW.VendorTicketCategories.Add(new VendorTicketCategoryDalDto
        {
            VendorId = access.Value.VendorId,
            TicketCategoryId = dto.TicketCategoryId,
            Notes = NormalizeOptional(dto.Notes)
        });

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return await BuildCategoryAssignmentListAsync(route, access.Value, cancellationToken);
    }

    public async Task<Result<VendorCategoryAssignmentListModel>> UpdateCategoryAssignmentAsync(
        VendorCategoryRoute route,
        VendorTicketCategoryBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveVendorAccessAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<VendorCategoryAssignmentListModel>(access.Errors);
        }

        if (route.TicketCategoryId == Guid.Empty)
        {
            return Result.Fail<VendorCategoryAssignmentListModel>(new NotFoundError(T(
                "VendorCategoryAssignmentWasNotFound",
                "Vendor category assignment was not found.")));
        }

        var validation = ValidateCategoryNotes(dto);
        if (validation.IsFailed)
        {
            return Result.Fail<VendorCategoryAssignmentListModel>(validation.Errors);
        }

        var existing = await ServiceUOW.VendorTicketCategories.FindByVendorCategoryInCompanyAsync(
            access.Value.VendorId,
            route.TicketCategoryId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (existing is null)
        {
            return Result.Fail<VendorCategoryAssignmentListModel>(new NotFoundError(T(
                "VendorCategoryAssignmentWasNotFound",
                "Vendor category assignment was not found.")));
        }

        await ServiceUOW.VendorTicketCategories.UpdateAsync(
            new VendorTicketCategoryDalDto
            {
                Id = existing.Id,
                VendorId = access.Value.VendorId,
                TicketCategoryId = route.TicketCategoryId,
                Notes = NormalizeOptional(dto.Notes)
            },
            access.Value.ManagementCompanyId,
            cancellationToken);

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return await BuildCategoryAssignmentListAsync(route, access.Value, cancellationToken);
    }

    public async Task<Result> RemoveCategoryAsync(
        VendorCategoryRoute route,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveVendorAccessAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail(access.Errors);
        }

        var deleted = await ServiceUOW.VendorTicketCategories.DeleteAssignmentAsync(
            access.Value.VendorId,
            route.TicketCategoryId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (!deleted)
        {
            return Result.Fail(new NotFoundError(T(
                "VendorCategoryAssignmentWasNotFound",
                "Vendor category assignment was not found.")));
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    private async Task<Result<VendorAccessContext>> ResolveVendorAccessAsync(
        VendorRoute route,
        HashSet<string> allowedRoleCodes,
        CancellationToken cancellationToken)
    {
        if (route.VendorId == Guid.Empty)
        {
            return Result.Fail<VendorAccessContext>(new NotFoundError(T("VendorWasNotFound", "Vendor was not found.")));
        }

        var company = await ResolveCompanyAccessAsync(route, allowedRoleCodes, cancellationToken);
        if (company.IsFailed)
        {
            return Result.Fail<VendorAccessContext>(company.Errors);
        }

        var exists = await ServiceUOW.Vendors.ExistsInCompanyAsync(
            route.VendorId,
            company.Value.ManagementCompanyId,
            cancellationToken);
        if (!exists)
        {
            return Result.Fail<VendorAccessContext>(new NotFoundError(T("VendorWasNotFound", "Vendor was not found.")));
        }

        return Result.Ok(new VendorAccessContext(
            company.Value.ManagementCompanyId,
            route.VendorId,
            company.Value.CompanySlug,
            company.Value.CompanyName));
    }

    private async Task<Result<VendorWorkspaceModel>> ResolveCompanyAccessAsync(
        ManagementCompanyRoute route,
        HashSet<string> allowedRoleCodes,
        CancellationToken cancellationToken)
    {
        if (route.AppUserId == Guid.Empty)
        {
            return Result.Fail<VendorWorkspaceModel>(new UnauthorizedError("Authentication is required."));
        }

        var company = await ServiceUOW.ManagementCompanies.FirstBySlugAsync(route.CompanySlug, cancellationToken);
        if (company is null)
        {
            return Result.Fail<VendorWorkspaceModel>(new NotFoundError(App.Resources.Views.UiText.ManagementCompanyWasNotFound));
        }

        var roleCode = await ServiceUOW.ManagementCompanies.FindActiveUserRoleCodeAsync(
            route.AppUserId,
            company.Id,
            cancellationToken);

        if (roleCode is null || !allowedRoleCodes.Contains(roleCode))
        {
            return Result.Fail<VendorWorkspaceModel>(new ForbiddenError(App.Resources.Views.UiText.AccessDeniedDescription));
        }

        return Result.Ok(new VendorWorkspaceModel
        {
            AppUserId = route.AppUserId,
            ManagementCompanyId = company.Id,
            CompanySlug = company.Slug,
            CompanyName = company.Name
        });
    }

    private static Result Validate(VendorBllDto dto)
    {
        var failures = new List<ValidationFailureModel>();

        AddRequiredFailure(failures, nameof(dto.Name), dto.Name, App.Resources.Views.UiText.Name);
        AddRequiredFailure(failures, nameof(dto.RegistryCode), dto.RegistryCode, App.Resources.Views.UiText.RegistryCode);
        AddRequiredFailure(failures, nameof(dto.Notes), dto.Notes, App.Resources.Views.UiText.Notes);

        return failures.Count == 0
            ? Result.Ok()
            : Result.Fail(new ValidationAppError("Validation failed.", failures));
    }

    private static void AddRequiredFailure(
        ICollection<ValidationFailureModel> failures,
        string propertyName,
        string? value,
        string displayName)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        failures.Add(new ValidationFailureModel
        {
            PropertyName = propertyName,
            ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", displayName)
        });
    }

    private static NormalizedVendor Normalize(VendorBllDto dto)
    {
        return new NormalizedVendor(
            dto.Name.Trim(),
            dto.RegistryCode.Trim(),
            dto.Notes.Trim());
    }

    private static VendorProfileModel ToProfileModel(VendorProfileDalDto profile)
    {
        return new VendorProfileModel
        {
            Id = profile.Id,
            ManagementCompanyId = profile.ManagementCompanyId,
            CompanySlug = profile.CompanySlug,
            CompanyName = profile.CompanyName,
            Name = profile.Name,
            RegistryCode = profile.RegistryCode,
            Notes = profile.Notes,
            CreatedAt = profile.CreatedAt,
            ActiveCategoryCount = profile.ActiveCategoryCount,
            AssignedTicketCount = profile.AssignedTicketCount,
            ContactCount = profile.ContactCount,
            ScheduledWorkCount = profile.ScheduledWorkCount
        };
    }

    private async Task<Result<VendorCategoryAssignmentListModel>> BuildCategoryAssignmentListAsync(
        VendorRoute route,
        VendorAccessContext access,
        CancellationToken cancellationToken)
    {
        var profile = await ServiceUOW.Vendors.FindProfileAsync(
            access.VendorId,
            access.ManagementCompanyId,
            cancellationToken);
        if (profile is null)
        {
            return Result.Fail<VendorCategoryAssignmentListModel>(new NotFoundError(T("VendorWasNotFound", "Vendor was not found.")));
        }

        var assignments = await ServiceUOW.VendorTicketCategories.AllByVendorAsync(
            access.VendorId,
            access.ManagementCompanyId,
            cancellationToken);
        var assignedCategoryIds = assignments
            .Select(assignment => assignment.TicketCategoryId)
            .ToHashSet();

        var categories = await ServiceUOW.Lookups.AllTicketCategoriesAsync(cancellationToken);

        return Result.Ok(new VendorCategoryAssignmentListModel
        {
            CompanySlug = access.CompanySlug,
            CompanyName = access.CompanyName,
            VendorId = access.VendorId,
            VendorName = profile.Name,
            Assignments = assignments.Select(ToCategoryAssignmentModel).ToList(),
            AvailableCategories = categories
                .Where(category => !assignedCategoryIds.Contains(category.Id))
                .Select(ToTicketOptionModel)
                .ToList()
        });
    }

    private async Task<Result> ValidateCategoryAssignmentAsync(
        VendorTicketCategoryBllDto dto,
        CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailureModel>();
        if (dto.TicketCategoryId == Guid.Empty)
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(dto.TicketCategoryId),
                ErrorMessage = T("TicketCategoryRequired", "Ticket category is required.")
            });
        }
        else if (!await ServiceUOW.Lookups.TicketCategoryExistsAsync(dto.TicketCategoryId, cancellationToken))
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(dto.TicketCategoryId),
                ErrorMessage = T("InvalidTicketCategory", "Selected ticket category is invalid.")
            });
        }

        AddCategoryNotesFailures(failures, dto.Notes);

        return failures.Count == 0
            ? Result.Ok()
            : Result.Fail(new ValidationAppError("Validation failed.", failures));
    }

    private static Result ValidateCategoryNotes(VendorTicketCategoryBllDto dto)
    {
        var failures = new List<ValidationFailureModel>();
        AddCategoryNotesFailures(failures, dto.Notes);

        return failures.Count == 0
            ? Result.Ok()
            : Result.Fail(new ValidationAppError("Validation failed.", failures));
    }

    private static void AddCategoryNotesFailures(
        ICollection<ValidationFailureModel> failures,
        string? notes)
    {
        if (!string.IsNullOrWhiteSpace(notes) && notes.Trim().Length > CategoryNotesMaxLength)
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(VendorTicketCategoryBllDto.Notes),
                ErrorMessage = T("ContactNotesMaxLength", "Notes must be 4000 characters or fewer.")
            });
        }
    }

    private static VendorCategoryAssignmentModel ToCategoryAssignmentModel(
        VendorCategoryAssignmentDalDto assignment)
    {
        return new VendorCategoryAssignmentModel
        {
            AssignmentId = assignment.Id,
            VendorId = assignment.VendorId,
            TicketCategoryId = assignment.TicketCategoryId,
            CategoryCode = assignment.CategoryCode,
            CategoryLabel = assignment.CategoryLabel,
            Notes = assignment.Notes,
            CreatedAt = assignment.CreatedAt
        };
    }

    private static App.BLL.DTO.Tickets.Models.TicketOptionModel ToTicketOptionModel(TicketOptionDalDto option)
    {
        return new App.BLL.DTO.Tickets.Models.TicketOptionModel
        {
            Id = option.Id,
            Code = option.Code,
            Label = option.Label
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string T(string key, string fallback)
    {
        return App.Resources.Views.UiText.ResourceManager.GetString(key) ?? fallback;
    }

    private sealed record VendorAccessContext(
        Guid ManagementCompanyId,
        Guid VendorId,
        string CompanySlug,
        string CompanyName);

    private sealed record NormalizedVendor(string Name, string RegistryCode, string Notes);
}
