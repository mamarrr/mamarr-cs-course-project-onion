namespace App.BLL.Contracts.Properties.Commands;

public sealed class DeletePropertyCommand
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string CustomerSlug { get; init; } = default!;
    public string PropertySlug { get; init; } = default!;
    public string ConfirmationName { get; init; } = default!;
}
