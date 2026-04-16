namespace App.DTO.v1.Shared;

public static class ApiErrorCodes
{
    public const string ValidationFailed = "validation_failed";
    public const string Unauthorized = "unauthorized";
    public const string Forbidden = "forbidden";
    public const string NotFound = "not_found";
    public const string Duplicate = "duplicate";
    public const string Conflict = "conflict";
    public const string BusinessRuleViolation = "business_rule_violation";
    public const string DeleteConfirmationMismatch = "delete_confirmation_mismatch";
}
