namespace Base.Contracts;

public interface IBaseMapper<TEntityOut, TEntityIn> : IMapper<TEntityIn, TEntityOut>
    where TEntityOut : class
    where TEntityIn : class
{
}
