using Base.BLL.Contracts;
using Base.DAL.Contracts;

namespace Base.BLL;

public class BaseBLL<TUOW> : IBaseBLL
    where TUOW : IBaseUOW
{
    protected readonly TUOW UOW;

    public BaseBLL(TUOW uow)
    {
        UOW = uow;
    }

    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await UOW.SaveChangesAsync(cancellationToken);
    }
}