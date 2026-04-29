using FluentResults;

namespace App.BLL.Contracts.Customers.Errors;

public sealed class DuplicateRegistryCodeError : Error
{
    public string PropertyName { get; }

    public DuplicateRegistryCodeError(string message, string propertyName) : base(message)
    {
        PropertyName = propertyName;
        Metadata["ErrorType"] = "Duplicate";
    }
}
