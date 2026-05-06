using FluentResults;

namespace App.BLL.DTO.Common.Errors;

public class UnexpectedAppError : Error
{
    public UnexpectedAppError(string message) : base(message)
    {
        Metadata["ErrorType"] = "Unexpected";
    }
}
