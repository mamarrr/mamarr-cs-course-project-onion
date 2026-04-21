using App.DAL.EF;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.ResidentWorkspace.Residents;

public class ManagementResidentService : IManagementResidentService
{
    private readonly AppDbContext _dbContext;

    public ManagementResidentService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ManagementResidentListResult> ListAsync(
        ManagementResidentsAuthorizedContext context,
        CancellationToken cancellationToken = default)
    {
        var residents = await _dbContext.Residents
            .AsNoTracking()
            .Where(r => r.ManagementCompanyId == context.ManagementCompanyId)
            .OrderBy(r => r.LastName)
            .ThenBy(r => r.FirstName)
            .ThenBy(r => r.IdCode)
            .Select(r => new ManagementResidentListItem
            {
                ResidentId = r.Id,
                FirstName = r.FirstName,
                LastName = r.LastName,
                FullName = string.Join(" ", new[] { r.FirstName, r.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))),
                IdCode = r.IdCode,
                PreferredLanguage = r.PreferredLanguage,
                IsActive = r.IsActive
            })
            .ToListAsync(cancellationToken);

        return new ManagementResidentListResult
        {
            Residents = residents
        };
    }

    public async Task<ManagementResidentCreateResult> CreateAsync(
        ManagementResidentsAuthorizedContext context,
        ManagementResidentCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedFirstName = request.FirstName?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedFirstName))
        {
            return new ManagementResidentCreateResult
            {
                InvalidFirstName = true,
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace(
                    "{0}",
                    App.Resources.Views.UiText.ResourceManager.GetString("FirstName") ?? "First name")
            };
        }

        var normalizedLastName = request.LastName?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedLastName))
        {
            return new ManagementResidentCreateResult
            {
                InvalidLastName = true,
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace(
                    "{0}",
                    App.Resources.Views.UiText.ResourceManager.GetString("LastName") ?? "Last name")
            };
        }

        var normalizedIdCode = request.IdCode?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedIdCode))
        {
            return new ManagementResidentCreateResult
            {
                InvalidIdCode = true,
                ErrorMessage = App.Resources.Views.UiText.RequiredField.Replace(
                    "{0}",
                    App.Resources.Views.UiText.ResourceManager.GetString("IdCode") ?? "ID code")
            };
        }

        var normalizedPreferredLanguage = string.IsNullOrWhiteSpace(request.PreferredLanguage)
            ? null
            : request.PreferredLanguage.Trim();

        var duplicateIdCode = await _dbContext.Residents
            .AsNoTracking()
            .AnyAsync(
                r => r.ManagementCompanyId == context.ManagementCompanyId && r.IdCode == normalizedIdCode,
                cancellationToken);

        if (duplicateIdCode)
        {
            return new ManagementResidentCreateResult
            {
                DuplicateIdCode = true,
                ErrorMessage = App.Resources.Views.UiText.ResourceManager.GetString("ResidentIdCodeAlreadyExists")
                               ?? "Resident with this ID code already exists in this company."
            };
        }

        var resident = new Resident
        {
            Id = Guid.NewGuid(),
            FirstName = normalizedFirstName,
            LastName = normalizedLastName,
            IdCode = normalizedIdCode,
            PreferredLanguage = normalizedPreferredLanguage,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ManagementCompanyId = context.ManagementCompanyId
        };

        _dbContext.Residents.Add(resident);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ManagementResidentCreateResult
        {
            Success = true,
            CreatedResidentId = resident.Id
        };
    }
}
