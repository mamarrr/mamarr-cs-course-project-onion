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
    private const string EntityNotFoundError = "Entity not found.";
    private const string EntityMappingError = "Entity mapping failed.";

    protected readonly TUOW ServiceUOW;
    protected readonly TRepository ServiceRepository;
    protected readonly IBaseMapper<TBLLEntity, TDALEntity> Mapper;

    public BaseService(TRepository serviceRepository, TUOW serviceUOW, IBaseMapper<TBLLEntity, TDALEntity> mapper)
    {
        ServiceUOW = serviceUOW;
        ServiceRepository = serviceRepository;
        Mapper = mapper;
    }


    public virtual async Task<Result<IEnumerable<TBLLEntity>>> AllAsync(
        TKey parentId = default!,
        CancellationToken cancellationToken = default)
    {
        var res = await ServiceRepository.AllAsync(parentId, cancellationToken);

        var mappedRes = new List<TBLLEntity>();
        foreach (var entity in res)
        {
            var mappedEntity = Mapper.Map(entity);
            if (mappedEntity == null)
            {
                return Result.Fail<IEnumerable<TBLLEntity>>(EntityMappingError);
            }

            mappedRes.Add(mappedEntity);
        }

        return Result.Ok<IEnumerable<TBLLEntity>>(mappedRes);
    }

    public virtual async Task<Result<TBLLEntity>> FindAsync(
        TKey id,
        TKey parentId = default!,
        CancellationToken cancellationToken = default)
    {
        var res = await ServiceRepository.FindAsync(id, parentId, cancellationToken);
        if (res == null)
        {
            return Result.Fail<TBLLEntity>(EntityNotFoundError);
        }

        var mappedEntity = Mapper.Map(res);
        return mappedEntity == null
            ? Result.Fail<TBLLEntity>(EntityMappingError)
            : Result.Ok(mappedEntity);
    }

    public virtual Result<TKey> Add(TBLLEntity entity)
    {
        var mappedEntity = Mapper.Map(entity);
        return mappedEntity == null
            ? Result.Fail<TKey>(EntityMappingError)
            : Result.Ok(ServiceRepository.Add(mappedEntity));
    }

    public virtual async Task<Result<TBLLEntity>> UpdateAsync(TBLLEntity entity, TKey parentId, CancellationToken cancellationToken = default)
    {
        var dalEntity = await ServiceRepository.FindAsync(entity.Id, parentId, cancellationToken);
        if (dalEntity == null)
        {
            return Result.Fail<TBLLEntity>(EntityNotFoundError);
        }

        var mappedEntity = Mapper.Map(entity);
        if (mappedEntity == null)
        {
            return Result.Fail<TBLLEntity>(EntityMappingError);
        }

        var res = await ServiceRepository.UpdateAsync(mappedEntity, parentId, cancellationToken);
        var mappedResult = Mapper.Map(res);
        return mappedResult == null
            ? Result.Fail<TBLLEntity>(EntityMappingError)
            : Result.Ok(mappedResult);
    }

    public virtual Result Remove(TBLLEntity entity)
    {
        var mappedEntity = Mapper.Map(entity);
        if (mappedEntity == null)
        {
            return Result.Fail(EntityMappingError);
        }

        ServiceRepository.Remove(mappedEntity);
        return Result.Ok();
    }

    public virtual async Task<Result> RemoveAsync(
        TKey id,
        TKey parentId = default!,
        CancellationToken cancellationToken = default)
    {
        var entity = await ServiceRepository.FindAsync(id, parentId, cancellationToken);
        if (entity == null)
        {
            return Result.Fail(EntityNotFoundError);
        }
        await ServiceRepository.RemoveAsync(id, parentId, cancellationToken);
        return Result.Ok();
    }
}
