using FluentResults;

namespace App.BLL.Contracts.Common.Errors;

public class ForbiddenError : Error
{
    public ForbiddenError(string message) : base(message)
    {
        Metadata["ErrorType"] = "Forbidden";
    }
}
