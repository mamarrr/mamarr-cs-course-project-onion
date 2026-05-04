namespace Base.Contracts;

public interface IBaseMapper<TEntityOut, TEntityIn>
    where TEntityOut : class
    where TEntityIn : class
{
    TEntityOut? Map(TEntityIn? entity);
    TEntityIn? Map(TEntityOut? entity);
}