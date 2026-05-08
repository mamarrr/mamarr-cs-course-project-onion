using App.BLL.Contracts.Common.Portal;
using App.BLL.Contracts.Vendors;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Contacts;
using App.BLL.DTO.Vendors;
using App.BLL.DTO.Vendors.Models;
using App.BLL.Mappers.Contacts;
using App.BLL.Mappers.Vendors;
using App.BLL.Services.Contacts;
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
    private const int VendorContactNameMaxLength = 200;
    private readonly IPortalContextProvider _portalContext;
    private readonly ContactWriter _contactWriter;
    private readonly ContactBllDtoMapper _contactMapper = new();

    public VendorService(
        IAppUOW uow,
        IPortalContextProvider portalContext,
        ContactWriter contactWriter)
        : base(uow.Vendors, uow, new VendorBllDtoMapper())
    {
        _portalContext = portalContext;
        _contactWriter = contactWriter;
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

    public async Task<Result<VendorContactListModel>> ListContactsAsync(
        VendorRoute route,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveVendorAccessAsync(route, ReadAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<VendorContactListModel>(access.Errors);
        }

        return await BuildContactListAsync(route, access.Value, cancellationToken);
    }

    public async Task<Result<VendorContactListModel>> AddContactAsync(
        VendorRoute route,
        VendorContactBllDto dto,
        ContactBllDto? newContact,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveVendorAccessAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<VendorContactListModel>(access.Errors);
        }

        var validation = await ValidateVendorContactAsync(
            dto,
            newContact,
            access.Value.ManagementCompanyId,
            access.Value.VendorId,
            null,
            cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<VendorContactListModel>(validation.Errors);
        }

        var transactionStarted = false;
        try
        {
            await ServiceUOW.BeginTransactionAsync(cancellationToken);
            transactionStarted = true;

            var contactId = dto.ContactId;
            if (newContact is not null)
            {
                var createdContact = await _contactWriter.StageCreateAsync(
                    access.Value.ManagementCompanyId,
                    newContact,
                    cancellationToken);
                if (createdContact.IsFailed)
                {
                    await ServiceUOW.RollbackTransactionAsync(cancellationToken);
                    transactionStarted = false;
                    return Result.Fail<VendorContactListModel>(createdContact.Errors);
                }

                contactId = createdContact.Value.Id;
            }

            if (dto.IsPrimary)
            {
                await ServiceUOW.VendorContacts.ClearPrimaryAsync(
                    access.Value.VendorId,
                    access.Value.ManagementCompanyId,
                    null,
                    cancellationToken);
            }

            var normalized = NormalizeVendorContact(dto);
            ServiceUOW.VendorContacts.Add(new VendorContactDalDto
            {
                VendorId = access.Value.VendorId,
                ContactId = contactId,
                ValidFrom = normalized.ValidFrom,
                ValidTo = normalized.ValidTo,
                Confirmed = normalized.Confirmed,
                IsPrimary = normalized.IsPrimary,
                FullName = normalized.FullName,
                RoleTitle = normalized.RoleTitle
            });

            await ServiceUOW.SaveChangesAsync(cancellationToken);
            await ServiceUOW.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            if (transactionStarted)
            {
                await ServiceUOW.RollbackTransactionAsync(cancellationToken);
            }
            throw;
        }

        return await BuildContactListAsync(route, access.Value, cancellationToken);
    }

    public async Task<Result<VendorContactListModel>> UpdateContactAsync(
        VendorContactRoute route,
        VendorContactBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveVendorContactAccessAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<VendorContactListModel>(access.Errors);
        }

        var validation = await ValidateVendorContactAsync(
            dto,
            null,
            access.Value.ManagementCompanyId,
            access.Value.VendorId,
            route.VendorContactId,
            cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<VendorContactListModel>(validation.Errors);
        }

        var transactionStarted = false;
        try
        {
            await ServiceUOW.BeginTransactionAsync(cancellationToken);
            transactionStarted = true;

            if (dto.IsPrimary)
            {
                await ServiceUOW.VendorContacts.ClearPrimaryAsync(
                    access.Value.VendorId,
                    access.Value.ManagementCompanyId,
                    route.VendorContactId,
                    cancellationToken);
            }

            var normalized = NormalizeVendorContact(dto);
            await ServiceUOW.VendorContacts.UpdateAsync(
                new VendorContactDalDto
                {
                    Id = route.VendorContactId,
                    VendorId = access.Value.VendorId,
                    ContactId = normalized.ContactId,
                    ValidFrom = normalized.ValidFrom,
                    ValidTo = normalized.ValidTo,
                    Confirmed = normalized.Confirmed,
                    IsPrimary = normalized.IsPrimary,
                    FullName = normalized.FullName,
                    RoleTitle = normalized.RoleTitle
                },
                access.Value.ManagementCompanyId,
                cancellationToken);

            await ServiceUOW.SaveChangesAsync(cancellationToken);
            await ServiceUOW.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            if (transactionStarted)
            {
                await ServiceUOW.RollbackTransactionAsync(cancellationToken);
            }
            throw;
        }

        return await BuildContactListAsync(route, access.Value, cancellationToken);
    }

    public async Task<Result> SetPrimaryContactAsync(
        VendorContactRoute route,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveVendorContactAccessAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail(access.Errors);
        }

        var existing = await ServiceUOW.VendorContacts.FindInCompanyAsync(
            route.VendorContactId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (existing is null)
        {
            return Result.Fail(new NotFoundError(T("VendorContactWasNotFound", "Vendor contact was not found.")));
        }

        var transactionStarted = false;
        try
        {
            await ServiceUOW.BeginTransactionAsync(cancellationToken);
            transactionStarted = true;
            await ServiceUOW.VendorContacts.ClearPrimaryAsync(
                access.Value.VendorId,
                access.Value.ManagementCompanyId,
                route.VendorContactId,
                cancellationToken);

            await ServiceUOW.VendorContacts.UpdateAsync(
                new VendorContactDalDto
                {
                    Id = existing.Id,
                    VendorId = existing.VendorId,
                    ContactId = existing.ContactId,
                    ValidFrom = existing.ValidFrom,
                    ValidTo = existing.ValidTo,
                    Confirmed = existing.Confirmed,
                    IsPrimary = true,
                    FullName = existing.FullName,
                    RoleTitle = existing.RoleTitle
                },
                access.Value.ManagementCompanyId,
                cancellationToken);

            await ServiceUOW.SaveChangesAsync(cancellationToken);
            await ServiceUOW.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            if (transactionStarted)
            {
                await ServiceUOW.RollbackTransactionAsync(cancellationToken);
            }
            throw;
        }

        return Result.Ok();
    }

    public async Task<Result> ConfirmContactAsync(
        VendorContactRoute route,
        CancellationToken cancellationToken = default)
    {
        return await SetContactConfirmationAsync(route, true, cancellationToken);
    }

    public async Task<Result> UnconfirmContactAsync(
        VendorContactRoute route,
        CancellationToken cancellationToken = default)
    {
        return await SetContactConfirmationAsync(route, false, cancellationToken);
    }

    public async Task<Result> RemoveContactAsync(
        VendorContactRoute route,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveVendorContactAccessAsync(route, DeleteAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail(access.Errors);
        }

        var deleted = await ServiceUOW.VendorContacts.DeleteInCompanyAsync(
            route.VendorContactId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (!deleted)
        {
            return Result.Fail(new NotFoundError(T("VendorContactWasNotFound", "Vendor contact was not found.")));
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

    private async Task<Result<VendorAccessContext>> ResolveVendorContactAccessAsync(
        VendorContactRoute route,
        HashSet<string> allowedRoleCodes,
        CancellationToken cancellationToken)
    {
        if (route.VendorContactId == Guid.Empty)
        {
            return Result.Fail<VendorAccessContext>(new NotFoundError(T("VendorContactWasNotFound", "Vendor contact was not found.")));
        }

        var access = await ResolveVendorAccessAsync(route, allowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<VendorAccessContext>(access.Errors);
        }

        var existing = await ServiceUOW.VendorContacts.FindInCompanyAsync(
            route.VendorContactId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (existing is null || existing.VendorId != access.Value.VendorId)
        {
            return Result.Fail<VendorAccessContext>(new NotFoundError(T("VendorContactWasNotFound", "Vendor contact was not found.")));
        }

        return access;
    }

    private async Task<Result<VendorContactListModel>> BuildContactListAsync(
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
            return Result.Fail<VendorContactListModel>(new NotFoundError(T("VendorWasNotFound", "Vendor was not found.")));
        }

        var contacts = await ServiceUOW.VendorContacts.AllByVendorAsync(
            access.VendorId,
            access.ManagementCompanyId,
            cancellationToken);
        var existingContacts = await ServiceUOW.Contacts.OptionsByCompanyAsync(
            access.ManagementCompanyId,
            cancellationToken);

        var contactTypes = await ServiceUOW.Lookups.AllContactTypesAsync(cancellationToken);

        return Result.Ok(new VendorContactListModel
        {
            CompanySlug = access.CompanySlug,
            CompanyName = access.CompanyName,
            VendorId = access.VendorId,
            VendorName = profile.Name,
            Contacts = contacts.Select(ToContactAssignmentModel).ToList(),
            ExistingContacts = existingContacts
                .Select(contact => _contactMapper.Map(contact)!)
                .ToList(),
            ContactTypes = contactTypes
                .Select(type => new App.BLL.DTO.Tickets.Models.TicketOptionModel
                {
                    Id = type.Id,
                    Code = type.Code,
                    Label = type.Label
                })
                .ToList()
        });
    }

    private async Task<Result> SetContactConfirmationAsync(
        VendorContactRoute route,
        bool confirmed,
        CancellationToken cancellationToken)
    {
        var access = await ResolveVendorContactAccessAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail(access.Errors);
        }

        var existing = await ServiceUOW.VendorContacts.FindInCompanyAsync(
            route.VendorContactId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (existing is null)
        {
            return Result.Fail(new NotFoundError(T("VendorContactWasNotFound", "Vendor contact was not found.")));
        }

        await ServiceUOW.VendorContacts.UpdateAsync(
            new VendorContactDalDto
            {
                Id = existing.Id,
                VendorId = existing.VendorId,
                ContactId = existing.ContactId,
                ValidFrom = existing.ValidFrom,
                ValidTo = existing.ValidTo,
                Confirmed = confirmed,
                IsPrimary = existing.IsPrimary,
                FullName = existing.FullName,
                RoleTitle = existing.RoleTitle
            },
            access.Value.ManagementCompanyId,
            cancellationToken);

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    private async Task<Result> ValidateVendorContactAsync(
        VendorContactBllDto dto,
        ContactBllDto? newContact,
        Guid managementCompanyId,
        Guid vendorId,
        Guid? exceptVendorContactId,
        CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailureModel>();
        AddVendorContactFailures(failures, dto);

        if (newContact is null)
        {
            if (dto.ContactId == Guid.Empty)
            {
                failures.Add(new ValidationFailureModel
                {
                    PropertyName = nameof(dto.ContactId),
                    ErrorMessage = T("ContactRequired", "Contact is required.")
                });
            }
            else if (!await ServiceUOW.Contacts.ExistsInCompanyAsync(dto.ContactId, managementCompanyId, cancellationToken))
            {
                failures.Add(new ValidationFailureModel
                {
                    PropertyName = nameof(dto.ContactId),
                    ErrorMessage = T("InvalidContact", "Selected contact is invalid.")
                });
            }
        }

        if (failures.Count > 0)
        {
            return Result.Fail(new ValidationAppError("Validation failed.", failures));
        }

        var contactId = dto.ContactId;
        if (newContact is null)
        {
            var duplicateLink = await ServiceUOW.VendorContacts.ContactLinkedToVendorAsync(
                vendorId,
                contactId,
                managementCompanyId,
                exceptVendorContactId,
                cancellationToken);
            if (duplicateLink)
            {
                return Result.Fail(new ConflictError(T(
                    "VendorContactAlreadyLinked",
                    "This contact is already linked to the vendor.")));
            }
        }

        return Result.Ok();
    }

    private static void AddVendorContactFailures(
        ICollection<ValidationFailureModel> failures,
        VendorContactBllDto dto)
    {
        if (dto.ValidFrom == default)
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(dto.ValidFrom),
                ErrorMessage = RequiredField(App.Resources.Views.UiText.ValidFrom)
            });
        }

        if (dto.ValidTo.HasValue && dto.ValidTo.Value < dto.ValidFrom)
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = nameof(dto.ValidTo),
                ErrorMessage = T("ValidToCannotBeBeforeValidFrom", "Valid to cannot be before valid from.")
            });
        }

        AddMaxLengthFailure(failures, nameof(dto.FullName), dto.FullName, VendorContactNameMaxLength);
        AddMaxLengthFailure(failures, nameof(dto.RoleTitle), dto.RoleTitle, VendorContactNameMaxLength);
    }

    private static void AddMaxLengthFailure(
        ICollection<ValidationFailureModel> failures,
        string propertyName,
        string? value,
        int maxLength)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            failures.Add(new ValidationFailureModel
            {
                PropertyName = propertyName,
                ErrorMessage = T("MaxLengthExceeded", $"Value must be {maxLength} characters or fewer.")
            });
        }
    }

    private static VendorContactBllDto NormalizeVendorContact(VendorContactBllDto dto)
    {
        return new VendorContactBllDto
        {
            Id = dto.Id,
            VendorId = dto.VendorId,
            ContactId = dto.ContactId,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            Confirmed = dto.Confirmed,
            IsPrimary = dto.IsPrimary,
            FullName = NormalizeOptional(dto.FullName),
            RoleTitle = NormalizeOptional(dto.RoleTitle)
        };
    }

    private static VendorContactAssignmentModel ToContactAssignmentModel(
        VendorContactAssignmentDalDto contact)
    {
        return new VendorContactAssignmentModel
        {
            VendorContactId = contact.Id,
            VendorId = contact.VendorId,
            ContactId = contact.ContactId,
            ContactTypeId = contact.ContactTypeId,
            ContactTypeCode = contact.ContactTypeCode,
            ContactTypeLabel = contact.ContactTypeLabel,
            ContactValue = contact.ContactValue,
            ContactNotes = contact.ContactNotes,
            ValidFrom = contact.ValidFrom,
            ValidTo = contact.ValidTo,
            Confirmed = contact.Confirmed,
            IsPrimary = contact.IsPrimary,
            FullName = contact.FullName,
            RoleTitle = contact.RoleTitle,
            CreatedAt = contact.CreatedAt
        };
    }

    private async Task<Result<VendorWorkspaceModel>> ResolveCompanyAccessAsync(
        ManagementCompanyRoute route,
        HashSet<string> allowedRoleCodes,
        CancellationToken cancellationToken)
    {
        var access = await _portalContext.ResolveCompanyWorkspaceAsync(
            route,
            allowedRoleCodes,
            cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<VendorWorkspaceModel>(access.Errors);
        }

        return Result.Ok(new VendorWorkspaceModel
        {
            AppUserId = access.Value.AppUserId,
            ManagementCompanyId = access.Value.ManagementCompanyId,
            CompanySlug = access.Value.CompanySlug,
            CompanyName = access.Value.CompanyName
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

    private static string RequiredField(string fieldName)
    {
        return App.Resources.Views.UiText.RequiredField.Replace("{0}", fieldName);
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
