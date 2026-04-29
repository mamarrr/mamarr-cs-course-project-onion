namespace Base.Contracts;

public interface IMapper<TLeft, TRight>
{
    TRight? Map(TLeft? source);

    TLeft? Map(TRight? source);
}
