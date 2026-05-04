using App.BLL.Contracts.Services;
using App.BLL.DTO;
using App.BLL.Mappers;
using App.DAL.Contracts;
using App.DAL.Contracts.Repositories;
using App.DAL.EF;
using App.DAL.EF.Repositories;
using Base.BLL;
using Base.Contracts;

namespace App.BLL.Services;

public class ListItemService
    : BaseService<App.BLL.DTO.ListItem, App.DAL.DTO.ListItem, IListItemRepository, IAppUOW>, IListItemService
{
    public ListItemService(IListItemRepository serviceRepository, IAppUOW serviceBLL,
        IBaseMapper<App.BLL.DTO.ListItem, App.DAL.DTO.ListItem> mapper)
        : base(serviceRepository, serviceBLL, mapper)
    {
    }
}