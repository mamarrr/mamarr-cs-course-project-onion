using App.BLL.Contracts.Common;
using FluentResults;

namespace App.BLL.Contracts.Common.Errors;

public sealed class ValidationAppError : Error
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
