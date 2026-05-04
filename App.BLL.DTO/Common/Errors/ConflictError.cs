using FluentResults;

namespace App.BLL.Contracts.Common.Errors;

public class ConflictError : Error
{
    public ConflictError(string message) : base(message)
    {
        Metadata["ErrorType"] = "Conflict";
    }
}
