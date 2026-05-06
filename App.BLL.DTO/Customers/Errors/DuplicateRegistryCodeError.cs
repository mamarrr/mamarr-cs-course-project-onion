using FluentResults;

namespace App.BLL.DTO.Customers.Errors;

public class DuplicateRegistryCodeError : Error
{
    public string PropertyName { get; }

    public DuplicateRegistryCodeError(string message, string propertyName) : base(message)
    {
        PropertyName = propertyName;
        Metadata["ErrorType"] = "Duplicate";
    }
}
