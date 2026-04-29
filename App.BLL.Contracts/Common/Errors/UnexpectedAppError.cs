using FluentResults;

namespace App.BLL.Contracts.Common.Errors;

public sealed class UnexpectedAppError : Error
{
    public UnexpectedAppError(string message) : base(message)
    {
        Metadata["ErrorType"] = "Unexpected";
    }
}
