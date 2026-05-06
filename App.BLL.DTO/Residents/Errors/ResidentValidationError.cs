using App.BLL.DTO.Common;
using FluentResults;

namespace App.BLL.DTO.Residents.Errors;

public class ResidentValidationError : Error
{
    public IReadOnlyList<ValidationFailureModel> Failures { get; }

    public ResidentValidationError(
        string message,
        IReadOnlyList<ValidationFailureModel> failures) : base(message)
    {
        Failures = failures;
        Metadata["ErrorType"] = "BusinessRule";
    }
}
