using App.BLL.Contracts.Units.Commands;
using App.BLL.Contracts.Units.Models;
using App.BLL.Contracts.Units.Queries;
using WebApp.ViewModels.Property;
using WebApp.ViewModels.Unit;

namespace WebApp.Mappers.Mvc.Units;

public class UnitMvcMapper
{
    public GetUnitDashboardQuery ToDashboardQuery(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        Guid appUserId)
    {
        return new GetUnitDashboardQuery
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UnitSlug = unitSlug,
            UserId = appUserId
        };
    }

    public GetPropertyUnitsQuery ToPropertyUnitsQuery(
        string companySlug,
        string customerSlug,
        string propertySlug,
        Guid appUserId)
    {
        return new GetPropertyUnitsQuery
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UserId = appUserId
        };
    }

    public GetUnitProfileQuery ToProfileQuery(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        Guid appUserId)
    {
        return new GetUnitProfileQuery
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UnitSlug = unitSlug,
            UserId = appUserId
        };
    }

    public CreateUnitCommand ToCreateCommand(
        string companySlug,
        string customerSlug,
        string propertySlug,
        AddUnitViewModel vm,
        Guid appUserId)
    {
        return new CreateUnitCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UserId = appUserId,
            UnitNr = vm.UnitNr,
            FloorNr = vm.FloorNr,
            SizeM2 = vm.SizeM2,
            Notes = vm.Notes
        };
    }

    public UpdateUnitCommand ToUpdateCommand(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        UnitProfileEditViewModel edit,
        Guid appUserId)
    {
        return new UpdateUnitCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UnitSlug = unitSlug,
            UserId = appUserId,
            UnitNr = edit.UnitNr,
            FloorNr = edit.FloorNr,
            SizeM2 = edit.SizeM2,
            Notes = edit.Notes,
        };
    }

    public DeleteUnitCommand ToDeleteCommand(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        UnitProfileEditViewModel edit,
        Guid appUserId)
    {
        return new DeleteUnitCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UnitSlug = unitSlug,
            UserId = appUserId,
            ConfirmationUnitNr = edit.DeleteConfirmation ?? string.Empty
        };
    }

    public UnitProfileEditViewModel ToEditViewModel(UnitProfileModel profile)
    {
        return new UnitProfileEditViewModel
        {
            UnitNr = profile.UnitNr,
            FloorNr = profile.FloorNr,
            SizeM2 = profile.SizeM2,
            Notes = profile.Notes,
        };
    }

}
