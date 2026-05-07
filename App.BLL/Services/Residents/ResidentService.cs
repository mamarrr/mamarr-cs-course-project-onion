using App.BLL.Contracts.Contacts;
using App.BLL.Contracts.Common.Deletion;
using App.BLL.Contracts.Residents;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Contacts;
using App.BLL.DTO.Residents;
using App.BLL.DTO.Residents.Errors;
using App.BLL.DTO.Residents.Models;
using App.BLL.Mappers.Residents;
using App.DAL.Contracts;
using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Residents;
using Base.BLL;
using FluentResults;

namespace App.BLL.Services.Residents;

public class ResidentService :
    BaseService<ResidentBllDto, ResidentDalDto, IResidentRepository, IAppUOW>,
    IResidentService
{
    private static readonly HashSet<string> DeleteAllowedRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER"
    };

    private static readonly HashSet<string> AccessAllowedRoleCodes = new(StringComparer.OrdinalIgnoreCase)
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

    private readonly IAppDeleteGuard _deleteGuard;
    private readonly IContactService _contactService;
    private readonly ResidentContactBllDtoMapper _residentContactMapper = new();

    public ResidentService(
        IAppUOW uow,
        IAppDeleteGuard deleteGuard,
        IContactService contactService)
        : base(uow.Residents, uow, new ResidentBllDtoMapper())
    {
        _deleteGuard = deleteGuard;
        _contactService = contactService;
    }

    public async Task<Result<CompanyResidentsModel>> ResolveCompanyResidentsContextAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default)
    {
        if (route.AppUserId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authentication is required."));
        }

        if (string.IsNullOrWhiteSpace(route.CompanySlug))
        {
            return Result.Fail(new NotFoundError(App.Resources.Views.UiText.ManagementCompanyWasNotFound));
        }

        var company = await ServiceUOW.ManagementCompanies.FirstBySlugAsync(
            route.CompanySlug,
            cancellationToken);
        if (company is null)
        {
            return Result.Fail(new NotFoundError(App.Resources.Views.UiText.ManagementCompanyWasNotFound));
        }

        var roleCode = await ServiceUOW.ManagementCompanies.FindActiveUserRoleCodeAsync(
            route.AppUserId,
            company.Id,
            cancellationToken);

        if (roleCode is null || !AccessAllowedRoleCodes.Contains(roleCode))
        {
            return Result.Fail(new ForbiddenError("Access denied."));
        }

        return Result.Ok(new CompanyResidentsModel
        {
            AppUserId = route.AppUserId,
            ManagementCompanyId = company.Id,
            CompanySlug = company.Slug,
            CompanyName = company.Name
        });
    }

    public async Task<Result<ResidentWorkspaceModel>> ResolveWorkspaceAsync(
        ResidentRoute route,
        CancellationToken cancellationToken = default)
    {
        if (route.AppUserId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authentication is required."));
        }

        if (string.IsNullOrWhiteSpace(route.CompanySlug))
        {
            return Result.Fail(new NotFoundError("Resident context was not found."));
        }

        var company = await ServiceUOW.ManagementCompanies.FirstBySlugAsync(
            route.CompanySlug,
            cancellationToken);
        if (company is null)
        {
            return Result.Fail(new NotFoundError("Resident context was not found."));
        }

        if (string.IsNullOrWhiteSpace(route.ResidentIdCode))
        {
            return Result.Fail(new NotFoundError("Resident context was not found."));
        }

        var resident = await ServiceUOW.Residents.FirstProfileAsync(
            route.CompanySlug,
            route.ResidentIdCode,
            cancellationToken);

        if (resident is null || resident.ManagementCompanyId != company.Id)
        {
            return Result.Fail(new NotFoundError("Resident context was not found."));
        }

        var roleCode = await ServiceUOW.ManagementCompanies.FindActiveUserRoleCodeAsync(
            route.AppUserId,
            company.Id,
            cancellationToken);
        if (roleCode is not null && AccessAllowedRoleCodes.Contains(roleCode))
        {
            return Result.Ok(new ResidentWorkspaceModel
            {
                AppUserId = route.AppUserId,
                ManagementCompanyId = resident.ManagementCompanyId,
                CompanySlug = resident.CompanySlug,
                CompanyName = resident.CompanyName,
                ResidentId = resident.Id,
                ResidentIdCode = resident.IdCode,
                FirstName = resident.FirstName,
                LastName = resident.LastName,
                FullName = BuildFullName(resident.FirstName, resident.LastName),
                PreferredLanguage = resident.PreferredLanguage
            });
        }

        var hasResidentContext = await ServiceUOW.Residents.HasActiveUserResidentContextAsync(
            route.AppUserId,
            resident.Id,
            cancellationToken);

        return hasResidentContext
            ? Result.Ok(new ResidentWorkspaceModel
            {
                AppUserId = route.AppUserId,
                ManagementCompanyId = resident.ManagementCompanyId,
                CompanySlug = resident.CompanySlug,
                CompanyName = resident.CompanyName,
                ResidentId = resident.Id,
                ResidentIdCode = resident.IdCode,
                FirstName = resident.FirstName,
                LastName = resident.LastName,
                FullName = BuildFullName(resident.FirstName, resident.LastName),
                PreferredLanguage = resident.PreferredLanguage
            })
            : Result.Fail(new ForbiddenError("Access denied."));
    }

    public async Task<Result<CompanyResidentsModel>> ListForCompanyAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default)
    {
        var company = await ResolveCompanyResidentsContextAsync(route, cancellationToken);
        if (company.IsFailed)
        {
            return Result.Fail(company.Errors);
        }

        var residents = await ServiceUOW.Residents.AllByCompanyAsync(
            company.Value.ManagementCompanyId,
            cancellationToken);

        return Result.Ok(new CompanyResidentsModel
        {
            AppUserId = company.Value.AppUserId,
            ManagementCompanyId = company.Value.ManagementCompanyId,
            CompanySlug = company.Value.CompanySlug,
            CompanyName = company.Value.CompanyName,
            Residents = residents.Select(resident => new ResidentListItemModel
            {
                ResidentId = resident.Id,
                FirstName = resident.FirstName,
                LastName = resident.LastName,
                FullName = BuildFullName(resident.FirstName, resident.LastName),
                IdCode = resident.IdCode,
                PreferredLanguage = resident.PreferredLanguage
            }).ToList()
        });
    }

    public async Task<Result<ResidentDashboardModel>> GetDashboardAsync(
        ResidentRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveWorkspaceAsync(route, cancellationToken);
        return workspace.IsFailed
            ? Result.Fail<ResidentDashboardModel>(workspace.Errors)
            : Result.Ok(new ResidentDashboardModel { Workspace = workspace.Value });
    }

    public async Task<Result<ResidentProfileModel>> GetProfileAsync(
        ResidentRoute route,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveWorkspaceAsync(route, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        return await GetProfileAsync(workspace.Value, cancellationToken);
    }

    public async Task<Result<ResidentBllDto>> CreateAsync(
        ManagementCompanyRoute route,
        ResidentBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var company = await ResolveCompanyResidentsContextAsync(route, cancellationToken);
        if (company.IsFailed)
        {
            return Result.Fail(company.Errors);
        }

        var validation = Validate(dto);
        if (validation.IsFailed)
        {
            return Result.Fail<ResidentBllDto>(validation.Errors);
        }

        var normalized = Normalize(dto);
        var duplicateIdCode = await ServiceUOW.Residents.IdCodeExistsForCompanyAsync(
            company.Value.ManagementCompanyId,
            normalized.IdCode,
            cancellationToken: cancellationToken);
        if (duplicateIdCode)
        {
            return Result.Fail(new DuplicateResidentIdCodeError(
                App.Resources.Views.UiText.ResourceManager.GetString("ResidentIdCodeAlreadyExists")
                ?? "Resident with this ID code already exists in this company.",
                nameof(dto.IdCode)));
        }

        dto.Id = Guid.Empty;
        dto.ManagementCompanyId = company.Value.ManagementCompanyId;
        dto.FirstName = normalized.FirstName;
        dto.LastName = normalized.LastName;
        dto.IdCode = normalized.IdCode;
        dto.PreferredLanguage = normalized.PreferredLanguage;

        return await AddAndFindCoreAsync(dto, company.Value.ManagementCompanyId, cancellationToken);
    }

    public async Task<Result<ResidentProfileModel>> CreateAndGetProfileAsync(
        ManagementCompanyRoute route,
        ResidentBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var created = await CreateAsync(route, dto, cancellationToken);
        if (created.IsFailed)
        {
            return Result.Fail<ResidentProfileModel>(created.Errors);
        }

        return await GetProfileAsync(
            new ResidentRoute
            {
                AppUserId = route.AppUserId,
                CompanySlug = route.CompanySlug,
                ResidentIdCode = created.Value.IdCode
            },
            cancellationToken);
    }

    public async Task<Result<ResidentBllDto>> UpdateAsync(
        ResidentRoute route,
        ResidentBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveWorkspaceAsync(route, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var validation = Validate(dto);
        if (validation.IsFailed)
        {
            return Result.Fail<ResidentBllDto>(validation.Errors);
        }

        var normalized = Normalize(dto);
        var duplicateIdCode = await ServiceUOW.Residents.IdCodeExistsForCompanyAsync(
            workspace.Value.ManagementCompanyId,
            normalized.IdCode,
            workspace.Value.ResidentId,
            cancellationToken);
        if (duplicateIdCode)
        {
            return Result.Fail(new DuplicateResidentIdCodeError(
                App.Resources.Views.UiText.ResourceManager.GetString("ResidentIdCodeAlreadyExists")
                ?? "Resident with this ID code already exists in this company.",
                nameof(dto.IdCode)));
        }

        dto.Id = workspace.Value.ResidentId;
        dto.ManagementCompanyId = workspace.Value.ManagementCompanyId;
        dto.FirstName = normalized.FirstName;
        dto.LastName = normalized.LastName;
        dto.IdCode = normalized.IdCode;
        dto.PreferredLanguage = normalized.PreferredLanguage;

        var updated = await base.UpdateAsync(dto, workspace.Value.ManagementCompanyId, cancellationToken);
        if (updated.IsFailed)
        {
            return Result.Fail<ResidentBllDto>(updated.Errors);
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return updated;
    }

    public async Task<Result<ResidentProfileModel>> UpdateAndGetProfileAsync(
        ResidentRoute route,
        ResidentBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var updated = await UpdateAsync(route, dto, cancellationToken);
        if (updated.IsFailed)
        {
            return Result.Fail<ResidentProfileModel>(updated.Errors);
        }

        return await GetProfileAsync(
            new ResidentRoute
            {
                AppUserId = route.AppUserId,
                CompanySlug = route.CompanySlug,
                ResidentIdCode = updated.Value.IdCode
            },
            cancellationToken);
    }

    public async Task<Result> DeleteAsync(
        ResidentRoute route,
        string confirmationIdCode,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveWorkspaceAsync(route, cancellationToken);
        if (workspace.IsFailed)
        {
            return Result.Fail(workspace.Errors);
        }

        var profile = await ServiceUOW.Residents.FindProfileAsync(
            workspace.Value.ResidentId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (profile is null)
        {
            return Result.Fail(new NotFoundError("Resident profile was not found."));
        }

        if (!string.Equals(confirmationIdCode?.Trim(), profile.IdCode.Trim(), StringComparison.Ordinal))
        {
            return Result.Fail(new ValidationAppError(
                "Delete confirmation does not match the current resident ID code.",
                [
                    new ValidationFailureModel
                    {
                        PropertyName = "ConfirmationIdCode",
                        ErrorMessage = "Delete confirmation does not match the current resident ID code."
                    }
                ]));
        }

        var roleCode = await ServiceUOW.ManagementCompanies.FindActiveUserRoleCodeAsync(
            route.AppUserId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (roleCode is null || !DeleteAllowedRoleCodes.Contains(roleCode))
        {
            return Result.Fail(new ForbiddenError("Access denied."));
        }

        var canDelete = await _deleteGuard.CanDeleteResidentAsync(
            workspace.Value.ResidentId,
            workspace.Value.ManagementCompanyId,
            cancellationToken);
        if (!canDelete)
        {
            return Result.Fail(new BusinessRuleError(DeleteBlockedMessage()));
        }

        var removed = await base.RemoveAsync(workspace.Value.ResidentId, workspace.Value.ManagementCompanyId, cancellationToken);
        if (removed.IsFailed)
        {
            return Result.Fail(removed.Errors);
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result<ResidentContactListModel>> ListContactsAsync(
        ResidentRoute route,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveResidentContactWorkflowAccessAsync(
            route,
            AccessAllowedRoleCodes,
            allowResidentContext: true,
            cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<ResidentContactListModel>(access.Errors);
        }

        return await BuildContactListAsync(route, access.Value, cancellationToken);
    }

    public async Task<Result<ResidentContactListModel>> AddContactAsync(
        ResidentRoute route,
        ResidentContactBllDto dto,
        ContactBllDto? newContact,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveResidentContactWorkflowAccessAsync(
            route,
            WriteAllowedRoleCodes,
            allowResidentContext: true,
            cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<ResidentContactListModel>(access.Errors);
        }

        var validation = await ValidateResidentContactAsync(
            dto,
            newContact,
            access.Value.ManagementCompanyId,
            access.Value.ResidentId,
            null,
            cancellationToken: cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<ResidentContactListModel>(validation.Errors);
        }

        var transactionStarted = false;
        try
        {
            await ServiceUOW.BeginTransactionAsync(cancellationToken);
            transactionStarted = true;

            var contactId = dto.ContactId;
            if (newContact is not null)
            {
                var createdContact = await _contactService.CreateAsync(route, newContact, cancellationToken);
                if (createdContact.IsFailed)
                {
                    await ServiceUOW.RollbackTransactionAsync(cancellationToken);
                    transactionStarted = false;
                    return Result.Fail<ResidentContactListModel>(createdContact.Errors);
                }

                contactId = createdContact.Value.Id;
            }

            if (dto.IsPrimary)
            {
                await ServiceUOW.ResidentContacts.ClearPrimaryAsync(
                    access.Value.ResidentId,
                    access.Value.ManagementCompanyId,
                    null,
                    cancellationToken);
            }

            var normalized = NormalizeResidentContact(dto, access.Value.ResidentId, contactId);
            var residentContactDalDto = _residentContactMapper.Map(normalized);
            if (residentContactDalDto is null)
            {
                await ServiceUOW.RollbackTransactionAsync(cancellationToken);
                transactionStarted = false;
                return Result.Fail<ResidentContactListModel>("Resident contact mapping failed.");
            }

            ServiceUOW.ResidentContacts.Add(residentContactDalDto);

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

    public async Task<Result<ResidentContactListModel>> UpdateContactAsync(
        ResidentContactRoute route,
        ResidentContactBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveResidentContactAccessAsync(
            route,
            WriteAllowedRoleCodes,
            allowResidentContext: true,
            cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<ResidentContactListModel>(access.Errors);
        }

        var validation = await ValidateResidentContactAsync(
            dto,
            null,
            access.Value.ManagementCompanyId,
            access.Value.ResidentId,
            route.ResidentContactId,
            cancellationToken: cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<ResidentContactListModel>(validation.Errors);
        }

        var transactionStarted = false;
        try
        {
            await ServiceUOW.BeginTransactionAsync(cancellationToken);
            transactionStarted = true;

            if (dto.IsPrimary)
            {
                await ServiceUOW.ResidentContacts.ClearPrimaryAsync(
                    access.Value.ResidentId,
                    access.Value.ManagementCompanyId,
                    route.ResidentContactId,
                    cancellationToken);
            }

            var normalized = NormalizeResidentContact(dto, access.Value.ResidentId, dto.ContactId);
            normalized.Id = route.ResidentContactId;
            var residentContactDalDto = _residentContactMapper.Map(normalized);
            if (residentContactDalDto is null)
            {
                await ServiceUOW.RollbackTransactionAsync(cancellationToken);
                transactionStarted = false;
                return Result.Fail<ResidentContactListModel>("Resident contact mapping failed.");
            }

            await ServiceUOW.ResidentContacts.UpdateAsync(
                residentContactDalDto,
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
        ResidentContactRoute route,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveResidentContactAccessAsync(
            route,
            WriteAllowedRoleCodes,
            allowResidentContext: true,
            cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail(access.Errors);
        }

        var existing = await ServiceUOW.ResidentContacts.FindInCompanyAsync(
            route.ResidentContactId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (existing is null)
        {
            return Result.Fail(new NotFoundError(T("ResidentContactWasNotFound", "Resident contact was not found.")));
        }

        var transactionStarted = false;
        try
        {
            await ServiceUOW.BeginTransactionAsync(cancellationToken);
            transactionStarted = true;

            await ServiceUOW.ResidentContacts.ClearPrimaryAsync(
                access.Value.ResidentId,
                access.Value.ManagementCompanyId,
                route.ResidentContactId,
                cancellationToken);

            var dto = _residentContactMapper.Map(existing);
            if (dto is null)
            {
                await ServiceUOW.RollbackTransactionAsync(cancellationToken);
                transactionStarted = false;
                return Result.Fail("Resident contact mapping failed.");
            }

            dto.IsPrimary = true;
            var dalDto = _residentContactMapper.Map(dto);
            if (dalDto is null)
            {
                await ServiceUOW.RollbackTransactionAsync(cancellationToken);
                transactionStarted = false;
                return Result.Fail("Resident contact mapping failed.");
            }

            await ServiceUOW.ResidentContacts.UpdateAsync(
                dalDto,
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
        ResidentContactRoute route,
        CancellationToken cancellationToken = default)
    {
        return await SetContactConfirmationAsync(route, true, cancellationToken);
    }

    public async Task<Result> UnconfirmContactAsync(
        ResidentContactRoute route,
        CancellationToken cancellationToken = default)
    {
        return await SetContactConfirmationAsync(route, false, cancellationToken);
    }

    public async Task<Result> RemoveContactAsync(
        ResidentContactRoute route,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveResidentContactAccessAsync(
            route,
            DeleteAllowedRoleCodes,
            allowResidentContext: false,
            cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail(access.Errors);
        }

        var deleted = await ServiceUOW.ResidentContacts.DeleteInCompanyAsync(
            route.ResidentContactId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (!deleted)
        {
            return Result.Fail(new NotFoundError(T("ResidentContactWasNotFound", "Resident contact was not found.")));
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    private async Task<Result<ResidentProfileModel>> GetProfileAsync(
        ResidentWorkspaceModel workspace,
        CancellationToken cancellationToken)
    {
        var profile = await ServiceUOW.Residents.FindProfileAsync(
            workspace.ResidentId,
            workspace.ManagementCompanyId,
            cancellationToken);

        return profile is null
            ? Result.Fail(new NotFoundError("Resident profile was not found."))
            : Result.Ok(new ResidentProfileModel
            {
                ResidentId = profile.Id,
                ManagementCompanyId = profile.ManagementCompanyId,
                CompanySlug = profile.CompanySlug,
                CompanyName = profile.CompanyName,
                ResidentIdCode = profile.IdCode,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                FullName = BuildFullName(profile.FirstName, profile.LastName),
                PreferredLanguage = profile.PreferredLanguage
            });
    }

    private async Task<Result<ResidentContactAccessContext>> ResolveResidentContactAccessAsync(
        ResidentContactRoute route,
        HashSet<string> allowedManagementRoleCodes,
        bool allowResidentContext,
        CancellationToken cancellationToken)
    {
        if (route.ResidentContactId == Guid.Empty)
        {
            return Result.Fail<ResidentContactAccessContext>(new NotFoundError(T("ResidentContactWasNotFound", "Resident contact was not found.")));
        }

        var access = await ResolveResidentContactWorkflowAccessAsync(
            route,
            allowedManagementRoleCodes,
            allowResidentContext,
            cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<ResidentContactAccessContext>(access.Errors);
        }

        var existing = await ServiceUOW.ResidentContacts.FindInCompanyAsync(
            route.ResidentContactId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (existing is null || existing.ResidentId != access.Value.ResidentId)
        {
            return Result.Fail<ResidentContactAccessContext>(new NotFoundError(T("ResidentContactWasNotFound", "Resident contact was not found.")));
        }

        return access;
    }

    private async Task<Result<ResidentContactAccessContext>> ResolveResidentContactWorkflowAccessAsync(
        ResidentRoute route,
        HashSet<string> allowedManagementRoleCodes,
        bool allowResidentContext,
        CancellationToken cancellationToken)
    {
        if (route.AppUserId == Guid.Empty)
        {
            return Result.Fail<ResidentContactAccessContext>(new UnauthorizedError("Authentication is required."));
        }

        if (string.IsNullOrWhiteSpace(route.CompanySlug) || string.IsNullOrWhiteSpace(route.ResidentIdCode))
        {
            return Result.Fail<ResidentContactAccessContext>(new NotFoundError("Resident context was not found."));
        }

        var profile = await ServiceUOW.Residents.FirstProfileAsync(
            route.CompanySlug,
            route.ResidentIdCode,
            cancellationToken);
        if (profile is null)
        {
            return Result.Fail<ResidentContactAccessContext>(new NotFoundError("Resident context was not found."));
        }

        var roleCode = await ServiceUOW.ManagementCompanies.FindActiveUserRoleCodeAsync(
            route.AppUserId,
            profile.ManagementCompanyId,
            cancellationToken);
        if (roleCode is not null && allowedManagementRoleCodes.Contains(roleCode))
        {
            return Result.Ok(ToResidentContactAccessContext(profile));
        }

        if (!allowResidentContext)
        {
            return Result.Fail<ResidentContactAccessContext>(new ForbiddenError("Access denied."));
        }

        var hasResidentContext = await ServiceUOW.Residents.HasActiveUserResidentContextAsync(
            route.AppUserId,
            profile.Id,
            cancellationToken);

        return hasResidentContext
            ? Result.Ok(ToResidentContactAccessContext(profile))
            : Result.Fail<ResidentContactAccessContext>(new ForbiddenError("Access denied."));
    }

    private async Task<Result<ResidentContactListModel>> BuildContactListAsync(
        ResidentRoute route,
        ResidentContactAccessContext access,
        CancellationToken cancellationToken)
    {
        var contacts = await ServiceUOW.ResidentContacts.AllByResidentAsync(
            access.ResidentId,
            access.ManagementCompanyId,
            cancellationToken);

        var existingContacts = await _contactService.ListForCompanyAsync(route, cancellationToken);
        if (existingContacts.IsFailed)
        {
            return Result.Fail<ResidentContactListModel>(existingContacts.Errors);
        }

        var contactTypes = await ServiceUOW.Lookups.AllContactTypesAsync(cancellationToken);

        return Result.Ok(new ResidentContactListModel
        {
            CompanySlug = access.CompanySlug,
            CompanyName = access.CompanyName,
            ResidentId = access.ResidentId,
            ResidentIdCode = access.ResidentIdCode,
            ResidentName = access.ResidentName,
            Contacts = contacts.Select(ToContactAssignmentModel).ToList(),
            ExistingContacts = existingContacts.Value,
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
        ResidentContactRoute route,
        bool confirmed,
        CancellationToken cancellationToken)
    {
        var access = await ResolveResidentContactAccessAsync(
            route,
            WriteAllowedRoleCodes,
            allowResidentContext: true,
            cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail(access.Errors);
        }

        var existing = await ServiceUOW.ResidentContacts.FindInCompanyAsync(
            route.ResidentContactId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (existing is null)
        {
            return Result.Fail(new NotFoundError(T("ResidentContactWasNotFound", "Resident contact was not found.")));
        }

        var dto = _residentContactMapper.Map(existing);
        if (dto is null)
        {
            return Result.Fail("Resident contact mapping failed.");
        }

        dto.Confirmed = confirmed;
        var dalDto = _residentContactMapper.Map(dto);
        if (dalDto is null)
        {
            return Result.Fail("Resident contact mapping failed.");
        }

        await ServiceUOW.ResidentContacts.UpdateAsync(
            dalDto,
            access.Value.ManagementCompanyId,
            cancellationToken);

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    private async Task<Result> ValidateResidentContactAsync(
        ResidentContactBllDto dto,
        ContactBllDto? newContact,
        Guid managementCompanyId,
        Guid residentId,
        Guid? exceptResidentContactId,
        CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailureModel>();
        AddResidentContactFailures(failures, dto);

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

        if (newContact is null)
        {
            var duplicateLink = await ServiceUOW.ResidentContacts.ContactLinkedToResidentAsync(
                residentId,
                dto.ContactId,
                managementCompanyId,
                exceptResidentContactId,
                cancellationToken);
            if (duplicateLink)
            {
                return Result.Fail(new ConflictError(T(
                    "ResidentContactAlreadyLinked",
                    "This contact is already linked to the resident.")));
            }
        }

        return Result.Ok();
    }

    private static void AddResidentContactFailures(
        ICollection<ValidationFailureModel> failures,
        ResidentContactBllDto dto)
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
    }

    private static ResidentContactBllDto NormalizeResidentContact(
        ResidentContactBllDto dto,
        Guid residentId,
        Guid contactId)
    {
        return new ResidentContactBllDto
        {
            Id = dto.Id,
            ResidentId = residentId,
            ContactId = contactId,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            Confirmed = dto.Confirmed,
            IsPrimary = dto.IsPrimary
        };
    }

    private static ResidentContactAssignmentModel ToContactAssignmentModel(
        ResidentContactAssignmentDalDto contact)
    {
        return new ResidentContactAssignmentModel
        {
            ResidentContactId = contact.Id,
            ResidentId = contact.ResidentId,
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
            CreatedAt = contact.CreatedAt
        };
    }

    private static Result Validate(ResidentBllDto dto)
    {
        var failures = new List<ValidationFailureModel>();

        if (string.IsNullOrWhiteSpace(dto.FirstName))
        {
            failures.Add(CreateRequiredFailure(nameof(dto.FirstName), App.Resources.Views.UiText.FirstName));
        }

        if (string.IsNullOrWhiteSpace(dto.LastName))
        {
            failures.Add(CreateRequiredFailure(nameof(dto.LastName), App.Resources.Views.UiText.LastName));
        }

        if (string.IsNullOrWhiteSpace(dto.IdCode))
        {
            failures.Add(CreateRequiredFailure(nameof(dto.IdCode), App.Resources.Views.UiText.IdCode));
        }

        return failures.Count == 0
            ? Result.Ok()
            : Result.Fail(new ResidentValidationError("Validation failed.", failures));
    }

    private static ValidationFailureModel CreateRequiredFailure(string propertyName, string displayName)
    {
        return new ValidationFailureModel
        {
            PropertyName = propertyName,
            ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", displayName)
        };
    }

    private static string RequiredField(string fieldName)
    {
        return App.Resources.Views.UiText.RequiredField.Replace("{0}", fieldName);
    }

    private static NormalizedResident Normalize(ResidentBllDto dto)
    {
        return new NormalizedResident(
            dto.FirstName.Trim(),
            dto.LastName.Trim(),
            dto.IdCode.Trim(),
            string.IsNullOrWhiteSpace(dto.PreferredLanguage)
                ? null
                : dto.PreferredLanguage.Trim());
    }

    private static string BuildFullName(string firstName, string lastName)
    {
        return string.Join(
            " ",
            new[] { firstName, lastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static ResidentContactAccessContext ToResidentContactAccessContext(ResidentProfileDalDto profile)
    {
        return new ResidentContactAccessContext(
            profile.ManagementCompanyId,
            profile.Id,
            profile.CompanySlug,
            profile.CompanyName,
            profile.IdCode,
            BuildFullName(profile.FirstName, profile.LastName));
    }

    private sealed record NormalizedResident(
        string FirstName,
        string LastName,
        string IdCode,
        string? PreferredLanguage);

    private sealed record ResidentContactAccessContext(
        Guid ManagementCompanyId,
        Guid ResidentId,
        string CompanySlug,
        string CompanyName,
        string ResidentIdCode,
        string ResidentName);

    private static string DeleteBlockedMessage()
    {
        return App.Resources.Views.UiText.ResourceManager.GetString("UnableToDeleteBecauseDependentRecordsExist")
               ?? "Unable to delete because dependent records exist.";
    }

    private static string T(string key, string fallback)
    {
        return App.Resources.Views.UiText.ResourceManager.GetString(key) ?? fallback;
    }
}
