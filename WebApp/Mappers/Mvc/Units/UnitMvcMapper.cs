using System.Security.Claims;
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
        ClaimsPrincipal user)
    {
        return new GetUnitDashboardQuery
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UnitSlug = unitSlug,
            UserId = GetAppUserId(user)
        };
    }

    public GetPropertyUnitsQuery ToPropertyUnitsQuery(
        string companySlug,
        string customerSlug,
        string propertySlug,
        ClaimsPrincipal user)
    {
        return new GetPropertyUnitsQuery
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UserId = GetAppUserId(user)
        };
    }

    public GetUnitProfileQuery ToProfileQuery(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        ClaimsPrincipal user)
    {
        return new GetUnitProfileQuery
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UnitSlug = unitSlug,
            UserId = GetAppUserId(user)
        };
    }

    public CreateUnitCommand ToCreateCommand(
        string companySlug,
        string customerSlug,
        string propertySlug,
        AddUnitViewModel vm,
        ClaimsPrincipal user)
    {
        return new CreateUnitCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UserId = GetAppUserId(user),
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
        ClaimsPrincipal user)
    {
        return new UpdateUnitCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UnitSlug = unitSlug,
            UserId = GetAppUserId(user),
            UnitNr = edit.UnitNr,
            FloorNr = edit.FloorNr,
            SizeM2 = edit.SizeM2,
            Notes = edit.Notes,
            IsActive = edit.IsActive
        };
    }

    public DeleteUnitCommand ToDeleteCommand(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        UnitProfileEditViewModel edit,
        ClaimsPrincipal user)
    {
        return new DeleteUnitCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UnitSlug = unitSlug,
            UserId = GetAppUserId(user),
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
            IsActive = profile.IsActive
        };
    }

    private static Guid GetAppUserId(ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : Guid.Empty;
    }
}
