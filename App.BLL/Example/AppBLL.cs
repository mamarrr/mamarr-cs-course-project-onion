using App.BLL.Contracts;
using App.BLL.Contracts.Services;
using App.BLL.Mappers;
using App.BLL.Services;
using App.DAL.Contracts;
using App.DAL.EF;
using Base.BLL;
using Base.BLL.Contracts;

namespace App.BLL;

public class AppBLL : BaseBLL<IAppUOW>,  IAppBLL
{
    public IListItemService ListItems => field ??= new ListItemService(UOW.ListItems, UOW, new ListItemBLLMapper());

    public AppBLL(IAppUOW uow) : base(uow)
    {
    }

}