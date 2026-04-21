using System.ComponentModel.DataAnnotations;
using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.Shared.Deletion;
using App.BLL.Shared.Profiles;
using App.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.CustomerWorkspace.Profiles;

public class ManagementCustomerProfileService : IManagementCustomerProfileService
{
    private readonly AppDbContext _dbContext;

    public ManagementCustomerProfileService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CustomerProfileModel?> GetProfileAsync(
        ManagementCustomerDashboardContext context,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .AsNoTracking()
            .Where(c => c.Id == context.CustomerId && c.ManagementCompanyId == context.ManagementCompanyId)
            .Select(c => new CustomerProfileModel
            {
                CustomerId = c.Id,
                CustomerSlug = c.Slug,
                Name = c.Name,
                RegistryCode = c.RegistryCode,
                BillingEmail = c.BillingEmail,
                BillingAddress = c.BillingAddress,
                Phone = c.Phone,
                IsActive = c.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProfileOperationResult> UpdateProfileAsync(
        ManagementCustomerDashboardContext context,
        CustomerProfileUpdateRequest request,
        CancellationToken cancellationToken = default)
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

        var normalizedName = request.Name.Trim();
        var normalizedRegistryCode = request.RegistryCode.Trim();
        var normalizedBillingEmail = string.IsNullOrWhiteSpace(request.BillingEmail) ? null : request.BillingEmail.Trim();
        var normalizedBillingAddress = string.IsNullOrWhiteSpace(request.BillingAddress) ? null : request.BillingAddress.Trim();
        var normalizedPhone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedBillingEmail))
        {
            var emailAttribute = new EmailAddressAttribute();
            if (!emailAttribute.IsValid(normalizedBillingEmail))
            {
                return new ProfileOperationResult
                {
                    ErrorMessage = App.Resources.Views.UiText.InvalidEmailAddress
                };
            }
        }

        var customer = await _dbContext.Customers
            .AsTracking()
            .FirstOrDefaultAsync(
                c => c.Id == context.CustomerId && c.ManagementCompanyId == context.ManagementCompanyId,
                cancellationToken);

        if (customer == null)
        {
            return new ProfileOperationResult { NotFound = true };
        }

        var duplicateRegistryCode = await _dbContext.Customers
            .AsNoTracking()
            .AnyAsync(
                c => c.Id != customer.Id &&
                     c.ManagementCompanyId == context.ManagementCompanyId &&
                     c.RegistryCode.ToLower() == normalizedRegistryCode.ToLower(),
                cancellationToken);

        if (duplicateRegistryCode)
        {
            return new ProfileOperationResult
            {
                DuplicateRegistryCode = true,
                ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("CustomerRegistryCodeAlreadyExists")
                               ?? "Customer with this registry code already exists in this company."
            };
        }

        customer.Name = normalizedName;
        customer.RegistryCode = normalizedRegistryCode;
        customer.BillingEmail = normalizedBillingEmail;
        customer.BillingAddress = normalizedBillingAddress;
        customer.Phone = normalizedPhone;
        customer.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProfileOperationResult { Success = true };
    }

    public async Task<ProfileOperationResult> DeleteProfileAsync(
        ManagementCustomerDashboardContext context,
        CancellationToken cancellationToken = default)
    {
        var hasDeleteRole = await ManagementProfileDeleteAuthorization.HasDeletePermissionAsync(
            _dbContext,
            context.ManagementCompanyId,
            context.AppUserId,
            cancellationToken);

        if (!hasDeleteRole)
        {
            return ManagementProfileDeleteAuthorization.ForbiddenResult();
        }

        var customer = await _dbContext.Customers
            .AsNoTracking()
            .Where(c => c.Id == context.CustomerId && c.ManagementCompanyId == context.ManagementCompanyId)
            .Select(c => new { c.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (customer == null)
        {
            return new ProfileOperationResult { NotFound = true };
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var propertyIds = await _dbContext.Properties
            .Where(p => p.CustomerId == customer.Id)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var unitIds = await _dbContext.Units
            .Where(u => propertyIds.Contains(u.PropertyId))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var ticketIds = await _dbContext.Tickets
            .Where(t => (t.CustomerId.HasValue && t.CustomerId.Value == customer.Id)
                        || (t.PropertyId.HasValue && propertyIds.Contains(t.PropertyId.Value))
                        || (t.UnitId.HasValue && unitIds.Contains(t.UnitId.Value)))
            .Where(t => t.ManagementCompanyId == context.ManagementCompanyId)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        await ManagementProfileDeleteOrchestrator.DeleteTicketsAsync(_dbContext, ticketIds, cancellationToken);

        await _dbContext.CustomerRepresentatives
            .Where(cr => cr.CustomerId == customer.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Leases
            .Where(l => unitIds.Contains(l.UnitId))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Units
            .Where(u => unitIds.Contains(u.Id))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Properties
            .Where(p => propertyIds.Contains(p.Id))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Customers
            .Where(c => c.Id == customer.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new ProfileOperationResult { Success = true };
    }
}

