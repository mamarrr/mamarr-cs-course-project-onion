using App.BLL.Contracts.Common;
using FluentResults;

namespace App.BLL.Contracts.Residents.Errors;

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
