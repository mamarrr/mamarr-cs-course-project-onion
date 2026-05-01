using FluentResults;

namespace App.BLL.Contracts.Common.Errors;

public class UnexpectedAppError : Error
{
    public UnexpectedAppError(string message) : base(message)
    {
        Metadata["ErrorType"] = "Unexpected";
    }
}
