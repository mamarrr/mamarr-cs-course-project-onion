
using Base.Contracts;
using Base.DAL.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Base.DAL.EF;

public class
    BaseRepository<TDALEntity, TDomainEntity, TDbContext> : BaseRepository<Guid, TDALEntity, TDomainEntity, TDbContext>
    where TDALEntity : class, IBaseEntity<Guid>
    where TDomainEntity : class, IBaseEntity<Guid>
    where TDbContext : DbContext
{
    public BaseRepository(TDbContext repositoryDbContext, IBaseMapper<TDALEntity, TDomainEntity> mapper) : base(
        repositoryDbContext, mapper)
    {
    }
}


public class BaseRepository<TKey, TDALEntity, TDomainEntity, TDbContext> : IBaseRepository<TKey, TDALEntity>
    where TDALEntity : class, IBaseEntity<TKey>
    where TDomainEntity : class, IBaseEntity<TKey>
    where TKey : IEquatable<TKey>
    where TDbContext : DbContext
{
    protected readonly TDbContext RepositoryDbContext;
    protected readonly DbSet<TDomainEntity> RepositoryDbSet;
    protected readonly IBaseMapper<TDALEntity, TDomainEntity> Mapper;

    public BaseRepository(TDbContext repositoryDbContext, IBaseMapper<TDALEntity, TDomainEntity> mapper)
    {
        RepositoryDbContext = repositoryDbContext;
        Mapper = mapper;
        RepositoryDbSet = RepositoryDbContext.Set<TDomainEntity>();
    }
    
    public virtual async Task<IEnumerable<TDALEntity>> AllAsync(TKey parentId = default!)
    {
        var query = RepositoryDbSet.AsQueryable();

        if (!parentId.Equals(default))
        {
            query = ApplyIdorRestrictions(query, parentId);
        }
        var domainRes = await query.ToListAsync();
        var res = domainRes.Select(e => Mapper.Map(e)!);
        return res;
    }

    public virtual async Task<TDALEntity?> FindAsync(TKey id, TKey parentId = default!)
    {
        var query = RepositoryDbSet.AsQueryable();
        if (!parentId.Equals(default))
        {
            query = ApplyIdorRestrictions(query, parentId);
        }
        var domainRes = await query.FirstOrDefaultAsync(e => e.Id.Equals(id));
        var res = Mapper.Map(domainRes);
        return res;
    }

    public virtual void Add(TDALEntity entity)
    {
        RepositoryDbSet.Add(Mapper.Map(entity)!);
    }

    public TDALEntity Update(TDALEntity entity)
    {
        return Mapper.Map(
            RepositoryDbSet.Update(
                Mapper.Map(entity)!
            ).Entity
        )!;
    }

    public void Remove(TDALEntity entity)
    {
        RepositoryDbSet.Remove(Mapper.Map(entity)!);
    }

    public async Task RemoveAsync(TKey id)
    {
        var entity = await RepositoryDbSet.FindAsync(id);
        if (entity != null) Remove(Mapper.Map(entity)!);
    }

    public async Task Remove(TKey id)
    {
        var entity = await FindAsync(id);
        if (entity != null) Remove(entity);
    }

    private IQueryable<TDomainEntity> ApplyIdorRestrictions(IQueryable<TDomainEntity> query, TKey parentId)
    {
        var res = CheckManagementCompanyId(query, parentId);
        res = CheckCustomerId(res, parentId);
        return res;
    }
    private IQueryable<TDomainEntity> CheckManagementCompanyId(IQueryable<TDomainEntity> query, TKey parentId)
    {
        if (typeof(IManagementCompanyId<TKey>).IsAssignableFrom(typeof(TDomainEntity)))
        {
            query = query.Where(e => ((IManagementCompanyId<TKey>)e).ManagementCompanyId.Equals(parentId));
        }
        return query;
    }

    private IQueryable<TDomainEntity> CheckCustomerId(IQueryable<TDomainEntity> query, TKey parentId)
    {
        if (typeof(ICustomerId<TKey>).IsAssignableFrom(typeof(TDomainEntity)))
        {
            query = query.Where(e => ((ICustomerId<TKey>)e).CustomerId.Equals(parentId));
        }
        return query;
    }
}