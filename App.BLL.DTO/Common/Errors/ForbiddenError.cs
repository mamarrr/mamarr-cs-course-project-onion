using FluentResults;

namespace App.BLL.DTO.Common.Errors;

public class ForbiddenError : Error
{
    public ForbiddenError(string message) : base(message)
    {
        Metadata["ErrorType"] = "Forbidden";
    }
}
