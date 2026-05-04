using App.DAL.Contracts.Repositories;
using App.DAL.DTO;
using Base.Contracts;
using Base.DAL.EF;

namespace App.DAL.EF.Repositories;

public class ListItemRepository : BaseRepository<App.DAL.DTO.ListItem, App.Domain.ListItem, AppDbContext>,
    IListItemRepository
{
    public ListItemRepository(AppDbContext repositoryDbContext,
        IBaseMapper<App.DAL.DTO.ListItem, Domain.ListItem> mapper) : base(repositoryDbContext, mapper)
    {
    }

    public override async Task<ListItem> UpdateAsync(ListItem entity, Guid appUserId = default)
    {
        var dbEntity = (await RepositoryDbSet.FindAsync(entity.Id))!;

        dbEntity.ItemDescription = entity.ItemDescription;
        dbEntity.IsDone = entity.IsDone;
        dbEntity.Summary.SetTranslation(entity.Summary);
        RepositoryDbContext.Update(dbEntity);

        return Mapper.Map(dbEntity)!;
    }
}