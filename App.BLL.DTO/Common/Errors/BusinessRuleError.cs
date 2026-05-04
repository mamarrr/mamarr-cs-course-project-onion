using FluentResults;

namespace App.BLL.Contracts.Common.Errors;

public class BusinessRuleError : Error
{
    public BusinessRuleError(string message) : base(message)
    {
        Metadata["ErrorType"] = "BusinessRule";
    }
}
