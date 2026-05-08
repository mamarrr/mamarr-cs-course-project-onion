using App.BLL.Contracts.Contacts;
using App.BLL.Contracts.Common.Portal;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Contacts;
using App.BLL.Mappers.Contacts;
using App.DAL.Contracts;
using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Contacts;
using Base.BLL;
using FluentResults;

namespace App.BLL.Services.Contacts;

public class ContactService :
    BaseService<ContactBllDto, ContactDalDto, IContactRepository, IAppUOW>,
    IContactService
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

    private readonly IPortalContextProvider _portalContext;
    private readonly ContactWriter _contactWriter;

    public ContactService(
        IAppUOW uow,
        IPortalContextProvider portalContext,
        ContactWriter contactWriter)
        : base(uow.Contacts, uow, new ContactBllDtoMapper())
    {
        _portalContext = portalContext;
        _contactWriter = contactWriter;
    }

    public async Task<Result<IReadOnlyList<ContactBllDto>>> ListForCompanyAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveCompanyAccessAsync(route, ReadAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<IReadOnlyList<ContactBllDto>>(access.Errors);
        }

        var contacts = await ServiceUOW.Contacts.OptionsByCompanyAsync(
            access.Value.ManagementCompanyId,
            cancellationToken);

        return Result.Ok((IReadOnlyList<ContactBllDto>)contacts
            .Select(contact => Mapper.Map(contact)!)
            .ToList());
    }

    public async Task<Result<ContactBllDto>> CreateAsync(
        ManagementCompanyRoute route,
        ContactBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveCompanyAccessAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<ContactBllDto>(access.Errors);
        }

        var created = await _contactWriter.StageCreateAsync(
            access.Value.ManagementCompanyId,
            dto,
            cancellationToken);
        if (created.IsFailed)
        {
            return Result.Fail<ContactBllDto>(created.Errors);
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return await FindAsync(created.Value.Id, access.Value.ManagementCompanyId, cancellationToken);
    }

    public async Task<Result<ContactBllDto>> UpdateAsync(
        ContactRoute route,
        ContactBllDto dto,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveCompanyAccessAsync(route, WriteAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<ContactBllDto>(access.Errors);
        }

        var existing = await ServiceUOW.Contacts.FindInCompanyAsync(
            route.ContactId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (existing is null)
        {
            return Result.Fail<ContactBllDto>(new NotFoundError(T("ContactNotFound", "Contact was not found.")));
        }

        var validation = await _contactWriter.ValidateAsync(dto, access.Value.ManagementCompanyId, route.ContactId, cancellationToken);
        if (validation.IsFailed)
        {
            return Result.Fail<ContactBllDto>(validation.Errors);
        }

        var normalized = _contactWriter.NormalizeForCompany(dto, access.Value.ManagementCompanyId);
        normalized.Id = route.ContactId;

        var updated = await base.UpdateAsync(normalized, access.Value.ManagementCompanyId, cancellationToken);
        if (updated.IsFailed)
        {
            return Result.Fail<ContactBllDto>(updated.Errors);
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return updated;
    }

    public async Task<Result> DeleteAsync(
        ContactRoute route,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveCompanyAccessAsync(route, DeleteAllowedRoleCodes, cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail(access.Errors);
        }

        var exists = await ServiceUOW.Contacts.ExistsInCompanyAsync(
            route.ContactId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (!exists)
        {
            return Result.Fail(new NotFoundError(T("ContactNotFound", "Contact was not found.")));
        }

        var hasDependencies = await ServiceUOW.Contacts.HasDependenciesAsync(
            route.ContactId,
            access.Value.ManagementCompanyId,
            cancellationToken);
        if (hasDependencies)
        {
            return Result.Fail(new BusinessRuleError(DeleteBlockedMessage()));
        }

        var removed = await base.RemoveAsync(route.ContactId, access.Value.ManagementCompanyId, cancellationToken);
        if (removed.IsFailed)
        {
            return Result.Fail(removed.Errors);
        }

        await ServiceUOW.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    private async Task<Result<CompanyAccessContext>> ResolveCompanyAccessAsync(
        ManagementCompanyRoute route,
        IReadOnlySet<string> allowedRoleCodes,
        CancellationToken cancellationToken)
    {
        var access = await _portalContext.ResolveCompanyWorkspaceAsync(
            route,
            allowedRoleCodes,
            cancellationToken);
        if (access.IsFailed)
        {
            return Result.Fail<CompanyAccessContext>(access.Errors);
        }

        return Result.Ok(new CompanyAccessContext(access.Value.ManagementCompanyId));
    }

    private static string DeleteBlockedMessage()
    {
        return App.Resources.Views.UiText.ResourceManager.GetString("UnableToDeleteBecauseDependentRecordsExist")
               ?? "Unable to delete because dependent records exist.";
    }

    private static string T(string key, string fallback)
    {
        return App.Resources.Views.UiText.ResourceManager.GetString(key) ?? fallback;
    }

    private sealed record CompanyAccessContext(Guid ManagementCompanyId);
}
