using FluentResults;

namespace App.BLL.DTO.Common.Errors;

public class ConflictError : Error
{
    public ConflictError(string message) : base(message)
    {
        Metadata["ErrorType"] = "Conflict";
    }
}
