namespace Base.DAL.EF;

public interface IBaseUOW
{
    Task<int> SaveChangesAsync();
}