using FluentResults;

namespace App.BLL.Contracts.Common.Errors;

public sealed class BusinessRuleError : Error
{
    public BusinessRuleError(string message) : base(message)
    {
        Metadata["ErrorType"] = "BusinessRule";
    }
}
