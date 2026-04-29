namespace App.BLL.Contracts.Customers.Models;

public sealed class CustomerPropertyLinkModel
{
    public string PropertySlug { get; init; } = default!;
    public string PropertyName { get; init; } = default!;
}
