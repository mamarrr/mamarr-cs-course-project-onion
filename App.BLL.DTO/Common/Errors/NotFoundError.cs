using FluentResults;

namespace App.BLL.DTO.Common.Errors;

public class NotFoundError : Error
{
    public NotFoundError(string message) : base(message)
    {
        Metadata["ErrorType"] = "NotFound";
    }
}
