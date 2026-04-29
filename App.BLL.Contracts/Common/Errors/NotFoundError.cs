using FluentResults;

namespace App.BLL.Contracts.Common.Errors;

public sealed class NotFoundError : Error
{
    public NotFoundError(string message) : base(message)
    {
        Metadata["ErrorType"] = "NotFound";
    }
}
