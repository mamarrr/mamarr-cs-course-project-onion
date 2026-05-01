using FluentResults;

namespace App.BLL.Contracts.Common.Errors;

public class NotFoundError : Error
{
    public NotFoundError(string message) : base(message)
    {
        Metadata["ErrorType"] = "NotFound";
    }
}
