using FluentResults;

namespace App.BLL.DTO.Common.Errors;

public class UnauthorizedError : Error
{
    public UnauthorizedError(string message) : base(message)
    {
        Metadata["ErrorType"] = "Unauthorized";
    }
}
