using Base.BLL.Contracts;
using Base.Contracts;
using Base.DAL.Contracts;
using FluentResults;

namespace Base.BLL;

public class
    BaseService<TBLLEntity, TDALEntity, TRepository, TUOW> :
    BaseService<Guid, TBLLEntity, TDALEntity, TRepository, TUOW> where TBLLEntity : class, IBaseEntity<Guid>
    where TDALEntity : class, IBaseEntity<Guid>
    where TRepository : IBaseRepository<Guid, TDALEntity>
    where TUOW: IBaseUOW
{
    public BaseService(TRepository serviceRepository, TUOW serviceBLL, IBaseMapper<TBLLEntity, TDALEntity> mapper) :
        base(serviceRepository, serviceBLL, mapper)
    {
    }
}

public class BaseService<TKey, TBLLEntity, TDALEntity, TRepository, TUOW> : IBaseService<TKey, TBLLEntity>
    where TBLLEntity : class, IBaseEntity<TKey>
    where TKey : IEquatable<TKey>
    where TRepository : IBaseRepository<TKey, TDALEntity>
    where TDALEntity : class, IBaseEntity<TKey>
    where TUOW: IBaseUOW
{
    protected readonly TUOW ServiceUOW;
    protected readonly TRepository ServiceRepository;
    protected readonly IBaseMapper<TBLLEntity, TDALEntity> Mapper;

    public BaseService(TRepository serviceRepository, TUOW serviceBLL, IBaseMapper<TBLLEntity, TDALEntity> mapper)
    {
        ServiceUOW = serviceBLL;
        ServiceRepository = serviceRepository;
        Mapper = mapper;
    }


    public virtual async Task<IEnumerable<TBLLEntity>> AllAsync(
        TKey parentId = default!,
        CancellationToken cancellationToken = default)
    {
        var res = await ServiceRepository.AllAsync(parentId, cancellationToken);
        var mappedRes = res.Select(e => Mapper.Map(e)!).ToList();
        return mappedRes;
    }

    public virtual async Task<TBLLEntity?> FindAsync(
        TKey id,
        TKey parentId = default!,
        CancellationToken cancellationToken = default)
    {
        var res = await ServiceRepository.FindAsync(id, parentId, cancellationToken);
        return Mapper.Map(res);
    }

    public virtual TKey Add(TBLLEntity entity)
    {
        return ServiceRepository.Add(Mapper.Map(entity)!);
    }

    public virtual async Task<TBLLEntity> UpdateAsync(TBLLEntity entity, TKey parentId, CancellationToken cancellationToken = default)
    {
        var res = await ServiceRepository.UpdateAsync(Mapper.Map(entity)!, parentId, cancellationToken);
        return Mapper.Map(res)!;
    }

    public virtual void Remove(TBLLEntity entity)
    {
        ServiceRepository.Remove(Mapper.Map(entity)!);
    }

    public virtual async Task RemoveAsync(
        TKey id,
        TKey parentId = default!,
        CancellationToken cancellationToken = default)
    {
        await ServiceRepository.RemoveAsync(id, parentId, cancellationToken);
    }
}
