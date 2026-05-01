namespace Base.Contracts;

public interface IMapper<TEntityOut, TEntityIn>
    where TEntityOut : class
    where TEntityIn : class
{
    TEntityOut? Map(TEntityIn? source);

    TEntityIn? Map(TEntityOut? source);
}
