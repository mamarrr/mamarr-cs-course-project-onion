using FluentResults;

namespace App.BLL.Contracts.Residents.Errors;

public sealed class DuplicateResidentIdCodeError : Error
{
    public string PropertyName { get; }

    public DuplicateResidentIdCodeError(string message, string propertyName) : base(message)
    {
        PropertyName = propertyName;
        Metadata["ErrorType"] = "Duplicate";
    }
}
