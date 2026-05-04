using App.BLL.Contracts.Services;
using Base.BLL.Contracts;

namespace App.BLL.Contracts;

public interface IAppBLL : IBaseBLL
{
    IListItemService ListItems { get; }
}