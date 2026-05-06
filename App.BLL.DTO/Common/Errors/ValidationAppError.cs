using FluentResults;

namespace App.BLL.DTO.Common.Errors;

public class ValidationAppError : Error
{
    public IReadOnlyList<ValidationFailureModel> Failures { get; }

    public ValidationAppError(
        string message,
        IReadOnlyList<ValidationFailureModel> failures) : base(message)
    {
        Failures = failures;
        Metadata["ErrorType"] = "Validation";
    }
}
