using Base.DAL.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Base.DAL.EF;

public class BaseUOW<TDbContext> : IBaseUOW
    where TDbContext : DbContext
{
    protected readonly TDbContext UowDbContext;

    public BaseUOW(TDbContext dbContext)
    {
        UowDbContext = dbContext;
    }
    
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await UowDbContext.SaveChangesAsync(cancellationToken);
    }
}
