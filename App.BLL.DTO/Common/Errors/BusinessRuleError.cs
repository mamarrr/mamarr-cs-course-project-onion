using FluentResults;

namespace App.BLL.DTO.Common.Errors;

public class BusinessRuleError : Error
{
    public BusinessRuleError(string message) : base(message)
    {
        Metadata["ErrorType"] = "BusinessRule";
    }
}
