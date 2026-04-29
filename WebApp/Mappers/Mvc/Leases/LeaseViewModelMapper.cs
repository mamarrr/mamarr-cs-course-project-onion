using App.BLL.Contracts.Leases.Models;
using WebApp.ViewModels.Resident;
using WebApp.ViewModels.Unit;

namespace WebApp.Mappers.Mvc.Leases;

public sealed class LeaseViewModelMapper
{
    public ResidentLeaseListItemViewModel ToResidentLeaseViewModel(ResidentLeaseModel lease)
    {
        return new ResidentLeaseListItemViewModel
        {
            LeaseId = lease.LeaseId,
            PropertyId = lease.PropertyId,
            PropertyName = lease.PropertyName,
            UnitId = lease.UnitId,
            UnitNr = lease.UnitNr,
            LeaseRoleId = lease.LeaseRoleId,
            LeaseRoleLabel = lease.LeaseRoleLabel,
            StartDate = lease.StartDate.ToDateTime(TimeOnly.MinValue),
            EndDate = lease.EndDate?.ToDateTime(TimeOnly.MinValue),
            IsActive = lease.IsActive,
            Notes = lease.Notes
        };
    }

    public UnitTenantLeaseListItemViewModel ToUnitLeaseViewModel(UnitLeaseModel lease)
    {
        return new UnitTenantLeaseListItemViewModel
        {
            LeaseId = lease.LeaseId,
            ResidentId = lease.ResidentId,
            ResidentFullName = lease.ResidentFullName,
            ResidentIdCode = lease.ResidentIdCode,
            LeaseRoleId = lease.LeaseRoleId,
            LeaseRoleLabel = lease.LeaseRoleLabel,
            StartDate = lease.StartDate.ToDateTime(TimeOnly.MinValue),
            EndDate = lease.EndDate?.ToDateTime(TimeOnly.MinValue),
            IsActive = lease.IsActive,
            Notes = lease.Notes
        };
    }
}
