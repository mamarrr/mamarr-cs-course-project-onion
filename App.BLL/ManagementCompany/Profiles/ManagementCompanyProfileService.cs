using System.ComponentModel.DataAnnotations;
using App.BLL.ManagementCompany.Membership;
using App.BLL.Shared.Deletion;
using App.BLL.Shared.Profiles;
using App.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.ManagementCompany.Profiles;

public class ManagementCompanyProfileService : IManagementCompanyProfileService
{
    private readonly AppDbContext _dbContext;
    private readonly IManagementUserAdminService _managementUserAdminService;

    public ManagementCompanyProfileService(
        AppDbContext dbContext,
        IManagementUserAdminService managementUserAdminService)
    {
        _dbContext = dbContext;
        _managementUserAdminService = managementUserAdminService;
    }

    public async Task<ManagementCompanyProfileModel?> GetProfileAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var auth = await _managementUserAdminService.AuthorizeManagementAreaAccessAsync(
            appUserId,
            companySlug,
            cancellationToken);

        if (!auth.IsAuthorized || auth.Context == null)
        {
            return null;
        }

        return await _dbContext.ManagementCompanies
            .AsNoTracking()
            .Where(mc => mc.Id == auth.Context.ManagementCompanyId)
            .Select(mc => new ManagementCompanyProfileModel
            {
                ManagementCompanyId = mc.Id,
                CompanySlug = mc.Slug,
                Name = mc.Name,
                RegistryCode = mc.RegistryCode,
                VatNumber = mc.VatNumber,
                Email = mc.Email,
                Phone = mc.Phone,
                Address = mc.Address,
                IsActive = mc.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProfileOperationResult> UpdateProfileAsync(
        Guid appUserId,
        string companySlug,
        ManagementCompanyProfileUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var auth = await _managementUserAdminService.AuthorizeManagementAreaAccessAsync(
            appUserId,
            companySlug,
            cancellationToken);

        if (auth.CompanyNotFound)
        {
            return new ProfileOperationResult { NotFound = true };
        }

        if (!auth.IsAuthorized || auth.Context == null)
        {
            return new ProfileOperationResult { Forbidden = true };
        }

        var validationError = ValidateRequiredCompanyFields(request);
        if (validationError != null)
        {
            return validationError;
        }

        var normalizedName = request.Name.Trim();
        var normalizedRegistryCode = request.RegistryCode.Trim();
        var normalizedVatNumber = request.VatNumber.Trim();
        var normalizedEmail = request.Email.Trim();
        var normalizedPhone = request.Phone.Trim();
        var normalizedAddress = request.Address.Trim();

        var emailAttribute = new EmailAddressAttribute();
        if (!emailAttribute.IsValid(normalizedEmail))
        {
            return new ProfileOperationResult
            {
                ErrorMessage = App.Resources.Views.UiText.InvalidEmailAddress
            };
        }

        var company = await _dbContext.ManagementCompanies
            .AsTracking()
            .FirstOrDefaultAsync(
                mc => mc.Id == auth.Context.ManagementCompanyId,
                cancellationToken);

        if (company == null)
        {
            return new ProfileOperationResult { NotFound = true };
        }

        var duplicateRegistryCode = await _dbContext.ManagementCompanies
            .AsNoTracking()
            .AnyAsync(
                mc => mc.Id != company.Id && mc.RegistryCode.ToLower() == normalizedRegistryCode.ToLower(),
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

        company.Name = normalizedName;
        company.RegistryCode = normalizedRegistryCode;
        company.VatNumber = normalizedVatNumber;
        company.Email = normalizedEmail;
        company.Phone = normalizedPhone;
        company.Address = normalizedAddress;
        company.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProfileOperationResult
        {
            Success = true
        };
    }

    public async Task<ProfileOperationResult> DeleteProfileAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default)
    {
        var auth = await _managementUserAdminService.AuthorizeManagementAreaAccessAsync(
            appUserId,
            companySlug,
            cancellationToken);

        if (auth.CompanyNotFound)
        {
            return new ProfileOperationResult { NotFound = true };
        }

        if (!auth.IsAuthorized || auth.Context == null)
        {
            return new ProfileOperationResult { Forbidden = true };
        }

        var hasDeleteRole = await ManagementProfileDeleteAuthorization.HasDeletePermissionAsync(
            _dbContext,
            auth.Context.ManagementCompanyId,
            appUserId,
            cancellationToken);

        if (!hasDeleteRole)
        {
            return ManagementProfileDeleteAuthorization.ForbiddenResult();
        }

        var company = await _dbContext.ManagementCompanies
            .AsNoTracking()
            .Where(mc => mc.Id == auth.Context.ManagementCompanyId)
            .Select(mc => new { mc.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (company == null)
        {
            return new ProfileOperationResult { NotFound = true };
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var customerIds = await _dbContext.Customers
            .Where(c => c.ManagementCompanyId == company.Id)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var propertyIds = await _dbContext.Properties
            .Where(p => customerIds.Contains(p.CustomerId))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var unitIds = await _dbContext.Units
            .Where(u => propertyIds.Contains(u.PropertyId))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var residentIds = await _dbContext.Residents
            .Where(r => r.ManagementCompanyId == company.Id)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var vendorIds = await _dbContext.Vendors
            .Where(v => v.ManagementCompanyId == company.Id)
            .Select(v => v.Id)
            .ToListAsync(cancellationToken);

        var ticketIds = await _dbContext.Tickets
            .Where(t => t.ManagementCompanyId == company.Id
                        || (t.CustomerId.HasValue && customerIds.Contains(t.CustomerId.Value))
                        || (t.PropertyId.HasValue && propertyIds.Contains(t.PropertyId.Value))
                        || (t.UnitId.HasValue && unitIds.Contains(t.UnitId.Value))
                        || (t.ResidentId.HasValue && residentIds.Contains(t.ResidentId.Value))
                        || (t.VendorId.HasValue && vendorIds.Contains(t.VendorId.Value)))
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        await ManagementProfileDeleteOrchestrator.DeleteTicketsAsync(_dbContext, ticketIds, cancellationToken);

        await _dbContext.CustomerRepresentatives
            .Where(cr => customerIds.Contains(cr.CustomerId) || residentIds.Contains(cr.ResidentId))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Leases
            .Where(l => unitIds.Contains(l.UnitId) || residentIds.Contains(l.ResidentId))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.ResidentUsers
            .Where(ru => residentIds.Contains(ru.ResidentId))
            .ExecuteDeleteAsync(cancellationToken);

        var residentContactIds = await _dbContext.ResidentContacts
            .Where(rc => residentIds.Contains(rc.ResidentId))
            .Select(rc => rc.ContactId)
            .ToListAsync(cancellationToken);

        var vendorContactIds = await _dbContext.VendorContacts
            .Where(vc => vendorIds.Contains(vc.VendorId))
            .Select(vc => vc.ContactId)
            .ToListAsync(cancellationToken);

        await _dbContext.ResidentContacts
            .Where(rc => residentIds.Contains(rc.ResidentId))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.VendorContacts
            .Where(vc => vendorIds.Contains(vc.VendorId))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.VendorTicketCategories
            .Where(vtc => vendorIds.Contains(vtc.VendorId))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Vendors
            .Where(v => vendorIds.Contains(v.Id))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Units
            .Where(u => unitIds.Contains(u.Id))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Properties
            .Where(p => propertyIds.Contains(p.Id))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Residents
            .Where(r => residentIds.Contains(r.Id))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Customers
            .Where(c => customerIds.Contains(c.Id))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.ManagementCompanyJoinRequests
            .Where(jr => jr.ManagementCompanyId == company.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.ManagementCompanyUsers
            .Where(mcu => mcu.ManagementCompanyId == company.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var companyContactIds = await _dbContext.Contacts
            .Where(c => c.ManagementCompanyId == company.Id)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var allContactIds = residentContactIds
            .Concat(vendorContactIds)
            .Concat(companyContactIds)
            .Distinct()
            .ToArray();

        await ManagementProfileDeleteOrchestrator.DeleteContactsIfOrphanedAsync(
            _dbContext,
            allContactIds,
            cancellationToken);

        await _dbContext.ManagementCompanies
            .Where(mc => mc.Id == company.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new ProfileOperationResult
        {
            Success = true
        };
    }

    private static ProfileOperationResult? ValidateRequiredCompanyFields(ManagementCompanyProfileUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new ProfileOperationResult
            {
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.Name)
            };
        }

        if (string.IsNullOrWhiteSpace(request.RegistryCode))
        {
            return new ProfileOperationResult
            {
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.RegistryCode)
            };
        }

        if (string.IsNullOrWhiteSpace(request.VatNumber))
        {
            return new ProfileOperationResult
            {
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.VatNumber)
            };
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return new ProfileOperationResult
            {
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.Email)
            };
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return new ProfileOperationResult
            {
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.Phone)
            };
        }

        if (string.IsNullOrWhiteSpace(request.Address))
        {
            return new ProfileOperationResult
            {
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace("{0}", App.Resources.Views.UiText.Address)
            };
        }

        return null;
    }
}

