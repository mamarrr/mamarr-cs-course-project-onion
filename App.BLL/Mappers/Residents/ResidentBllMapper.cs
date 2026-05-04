using App.BLL.Contracts.Residents.Models;
using App.DAL.DTO.Residents;

namespace App.BLL.Mappers.Residents;

public static class ResidentBllMapper
{
    public static ResidentWorkspaceModel MapWorkspace(
        Guid appUserId,
        ResidentProfileDalDto resident)
    {
        return new ResidentWorkspaceModel
        {
            AppUserId = appUserId,
            ManagementCompanyId = resident.ManagementCompanyId,
            CompanySlug = resident.CompanySlug,
            CompanyName = resident.CompanyName,
            ResidentId = resident.Id,
            ResidentIdCode = resident.IdCode,
            FirstName = resident.FirstName,
            LastName = resident.LastName,
            FullName = BuildFullName(resident.FirstName, resident.LastName),
            PreferredLanguage = resident.PreferredLanguage,
            IsActive = resident.IsActive
        };
    }

    public static ResidentProfileModel MapProfile(ResidentProfileDalDto resident)
    {
        return new ResidentProfileModel
        {
            ResidentId = resident.Id,
            ManagementCompanyId = resident.ManagementCompanyId,
            CompanySlug = resident.CompanySlug,
            CompanyName = resident.CompanyName,
            ResidentIdCode = resident.IdCode,
            FirstName = resident.FirstName,
            LastName = resident.LastName,
            FullName = BuildFullName(resident.FirstName, resident.LastName),
            PreferredLanguage = resident.PreferredLanguage,
            IsActive = resident.IsActive
        };
    }

    public static ResidentListItemModel MapListItem(ResidentListItemDalDto resident)
    {
        return new ResidentListItemModel
        {
            ResidentId = resident.Id,
            FirstName = resident.FirstName,
            LastName = resident.LastName,
            FullName = BuildFullName(resident.FirstName, resident.LastName),
            IdCode = resident.IdCode,
            PreferredLanguage = resident.PreferredLanguage,
            IsActive = resident.IsActive
        };
    }

    public static ResidentContactModel MapContact(ResidentContactDalDto contact)
    {
        return new ResidentContactModel
        {
            ResidentContactId = contact.ResidentContactId,
            ContactId = contact.ContactId,
            ContactTypeId = contact.ContactTypeId,
            ContactTypeCode = contact.ContactTypeCode,
            ContactTypeLabel = contact.ContactTypeLabel,
            ContactValue = contact.ContactValue,
            Notes = contact.Notes,
            ValidFrom = contact.ValidFrom,
            ValidTo = contact.ValidTo,
            Confirmed = contact.Confirmed,
            IsPrimary = contact.IsPrimary
        };
    }

    public static ResidentLeaseSummaryModel MapLeaseSummary(ResidentLeaseSummaryDalDto lease)
    {
        return new ResidentLeaseSummaryModel
        {
            LeaseId = lease.LeaseId,
            ResidentId = lease.ResidentId,
            UnitId = lease.UnitId,
            PropertyId = lease.PropertyId,
            PropertyName = lease.PropertyName,
            PropertySlug = lease.PropertySlug,
            UnitNr = lease.UnitNr,
            UnitSlug = lease.UnitSlug,
            LeaseRoleId = lease.LeaseRoleId,
            LeaseRoleCode = lease.LeaseRoleCode,
            LeaseRoleLabel = lease.LeaseRoleLabel,
            StartDate = lease.StartDate,
            EndDate = lease.EndDate,
            IsActive = lease.IsActive,
            Notes = lease.Notes
        };
    }

    private static string BuildFullName(string firstName, string lastName)
    {
        return string.Join(
            " ",
            new[] { firstName, lastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
