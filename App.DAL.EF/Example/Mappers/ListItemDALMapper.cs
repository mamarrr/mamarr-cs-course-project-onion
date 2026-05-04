using App.DAL.DTO;
using Base.Contracts;

namespace App.DAL.EF.Mappers;

public class ListItemDALMapper : IBaseMapper<App.DAL.DTO.ListItem, Domain.ListItem>
{
    public ListItem? Map(Domain.ListItem? entity)
    {
        if (entity == null) return null;
        var res = new App.DAL.DTO.ListItem()
        {
            Id = entity.Id,
            ItemDescription = entity.ItemDescription,
            Summary = entity.Summary.Translate() ?? "",
            IsDone = entity.IsDone,
        };
        return res;
    }

    public Domain.ListItem? Map(ListItem? entity)
    {
        if (entity == null) return null;
        var res = new Domain.ListItem()
        {
            Id = entity.Id,
            ItemDescription = entity.ItemDescription,
            Summary = entity.Summary,
            IsDone = entity.IsDone,
        };
        return res;
    }
}