namespace App.BLL.Contracts.Common;

public class ValidationFailureModel
{
    public string PropertyName { get; init; } = default!;
    public string ErrorMessage { get; init; } = default!;
}
