using System.Security.Claims;
using App.BLL.Contracts.Units.Commands;
using App.BLL.Contracts.Units.Models;
using App.BLL.Contracts.Units.Queries;
using App.DTO.v1.Property;
using App.DTO.v1.Shared;
using App.DTO.v1.Unit;

namespace WebApp.Mappers.Api.Units;

public class UnitApiMapper
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

    public CreateUnitCommand ToCreateCommand(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CreatePropertyUnitRequestDto dto,
        ClaimsPrincipal user)
    {
        return new CreateUnitCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UserId = GetAppUserId(user),
            UnitNr = dto.UnitNr,
            FloorNr = dto.FloorNr,
            SizeM2 = dto.SizeM2,
            Notes = dto.Notes
        };
    }

    public UpdateUnitCommand ToUpdateCommand(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        UpdateUnitProfileRequestDto dto,
        ClaimsPrincipal user)
    {
        return new UpdateUnitCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UnitSlug = unitSlug,
            UserId = GetAppUserId(user),
            UnitNr = dto.UnitNr,
            FloorNr = dto.FloorNr,
            SizeM2 = dto.SizeM2,
            Notes = dto.Notes,
        };
    }

    public DeleteUnitCommand ToDeleteCommand(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        DeleteUnitProfileRequestDto dto,
        ClaimsPrincipal user)
    {
        return new DeleteUnitCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UnitSlug = unitSlug,
            UserId = GetAppUserId(user),
            ConfirmationUnitNr = dto.ConfirmationUnitNr
        };
    }

    public PropertyUnitsResponseDto ToPropertyUnitsResponseDto(PropertyUnitsModel model)
    {
        return new PropertyUnitsResponseDto
        {
            Units = model.Units.Select(unit => new PropertyUnitSummaryDto
            {
                UnitId = unit.UnitId,
                UnitSlug = unit.UnitSlug,
                UnitNr = unit.UnitNr,
                FloorNr = unit.FloorNr,
                SizeM2 = unit.SizeM2,
                RouteContext = CreateUnitRouteContext(
                    model.CompanySlug,
                    model.CompanyName,
                    model.CustomerSlug,
                    model.CustomerName,
                    model.PropertySlug,
                    model.PropertyName,
                    unit.UnitSlug,
                    unit.UnitNr,
                    "property-units")
            }).ToList()
        };
    }

    public CreatePropertyUnitResponseDto ToCreateResponseDto(UnitProfileModel model)
    {
        return new CreatePropertyUnitResponseDto
        {
            UnitId = model.UnitId,
            UnitSlug = model.UnitSlug,
            RouteContext = CreateUnitRouteContext(
                model.CompanySlug,
                model.CompanyName,
                model.CustomerSlug,
                model.CustomerName,
                model.PropertySlug,
                model.PropertyName,
                model.UnitSlug,
                model.UnitNr,
                "property-units")
        };
    }

    public UnitDashboardResponseDto ToDashboardResponseDto(UnitDashboardModel model)
    {
        return new UnitDashboardResponseDto
        {
            Dashboard = new ApiDashboardDto
            {
                RouteContext = CreateUnitRouteContext(model.Workspace, "unit-dashboard"),
                Title = model.Title,
                SectionLabel = model.SectionLabel,
                Widgets = model.Widgets
            }
        };
    }

    public UnitProfileResponseDto ToProfileResponseDto(UnitProfileModel model)
    {
        return new UnitProfileResponseDto
        {
            Profile = new UnitProfileDto
            {
                UnitId = model.UnitId,
                UnitSlug = model.UnitSlug,
                UnitNr = model.UnitNr,
                FloorNr = model.FloorNr,
                SizeM2 = model.SizeM2,
                Notes = model.Notes,
                RouteContext = CreateUnitRouteContext(
                    model.CompanySlug,
                    model.CompanyName,
                    model.CustomerSlug,
                    model.CustomerName,
                    model.PropertySlug,
                    model.PropertyName,
                    model.UnitSlug,
                    model.UnitNr,
                    "unit-profile")
            }
        };
    }

    public ApiRouteContextDto CreateUnitRouteContext(
        UnitWorkspaceModel context,
        string currentSection)
    {
        return CreateUnitRouteContext(
            context.CompanySlug,
            context.CompanyName,
            context.CustomerSlug,
            context.CustomerName,
            context.PropertySlug,
            context.PropertyName,
            context.UnitSlug,
            context.UnitNr,
            currentSection);
    }

    private static ApiRouteContextDto CreateUnitRouteContext(
        string companySlug,
        string companyName,
        string customerSlug,
        string customerName,
        string propertySlug,
        string propertyName,
        string unitSlug,
        string unitName,
        string currentSection)
    {
        return new ApiRouteContextDto
        {
            CompanySlug = companySlug,
            CompanyName = companyName,
            CustomerSlug = customerSlug,
            CustomerName = customerName,
            PropertySlug = propertySlug,
            PropertyName = propertyName,
            UnitSlug = unitSlug,
            UnitName = unitName,
            CurrentSection = currentSection
        };
    }

    private static Guid GetAppUserId(ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : Guid.Empty;
    }
}
