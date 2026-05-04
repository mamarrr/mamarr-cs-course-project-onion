using Base.Contracts;

namespace App.BLL.Mappers;

public class ListItemBLLMapper : IBaseMapper<App.BLL.DTO.ListItem, App.DAL.DTO.ListItem>
{
    public App.BLL.DTO.ListItem? Map(App.DAL.DTO.ListItem? entity)
    {
        if (entity == null) return null;
        var res = new App.BLL.DTO.ListItem()
        {
            Id = entity.Id,
            ItemDescription = entity.ItemDescription,
            Summary = entity.Summary,
            IsDone = entity.IsDone,
        };
        return res;
    }

    public App.DAL.DTO.ListItem? Map(App.BLL.DTO.ListItem? entity)
    {
        if (entity == null) return null;
        var res = new App.DAL.DTO.ListItem()
        {
            Id = entity.Id,
            ItemDescription = entity.ItemDescription,
            Summary = entity.Summary,
            IsDone = entity.IsDone,
        };
        return res;
    }
}