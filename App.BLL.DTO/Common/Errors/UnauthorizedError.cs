using FluentResults;

namespace App.BLL.Contracts.Common.Errors;

public class UnauthorizedError : Error
{
    public UnauthorizedError(string message) : base(message)
    {
        Metadata["ErrorType"] = "Unauthorized";
    }
}
