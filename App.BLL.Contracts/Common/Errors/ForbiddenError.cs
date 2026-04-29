using FluentResults;

namespace App.BLL.Contracts.Common.Errors;

public sealed class ForbiddenError : Error
{
    public ForbiddenError(string message) : base(message)
    {
        Metadata["ErrorType"] = "Forbidden";
    }
}
