using App.BLL.DTO.Properties.Models;
using App.BLL.DTO.Tickets.Models;
using App.DTO.v1.Shared;

namespace App.DTO.v1.Mappers.Portal.Lookups;

public class LookupApiMapper
{
    public IReadOnlyList<LookupOptionDto> MapPropertyTypes(IReadOnlyList<PropertyTypeOptionModel> models)
    {
        return models.Select(MapPropertyType).ToList();
    }

    public IReadOnlyList<LookupOptionDto> MapTicketOptions(IReadOnlyList<TicketOptionModel> models)
    {
        return models.Select(MapTicketOption).ToList();
    }

    private static LookupOptionDto MapPropertyType(PropertyTypeOptionModel model)
    {
        return new LookupOptionDto
        {
            Id = model.Id,
            Code = model.Code,
            Label = model.Label
        };
    }

    private static LookupOptionDto MapTicketOption(TicketOptionModel model)
    {
        return new LookupOptionDto
        {
            Id = model.Id,
            Code = model.Code,
            Label = model.Label
        };
    }
}
