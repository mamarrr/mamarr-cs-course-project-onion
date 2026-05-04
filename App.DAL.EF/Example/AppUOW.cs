using App.DAL.Contracts;
using App.DAL.Contracts.Repositories;
using App.DAL.EF.Mappers;
using App.DAL.EF.Repositories;
using Base.DAL.EF;

namespace App.DAL.EF;

public class AppUOW : BaseUOW<AppDbContext>, IAppUOW
{
    public IListItemRepository ListItems => field ??= new ListItemRepository(UowDbContext, new ListItemDALMapper());

    public AppUOW(AppDbContext dbContext) : base(dbContext)
    {
    }
}