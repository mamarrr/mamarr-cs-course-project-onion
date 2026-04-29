using FluentResults;

namespace App.BLL.Contracts.Common.Errors;

public sealed class UnauthorizedError : Error
{
    public UnauthorizedError(string message) : base(message)
    {
        Metadata["ErrorType"] = "Unauthorized";
    }
}
