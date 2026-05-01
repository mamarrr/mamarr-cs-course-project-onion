namespace App.BLL.Contracts.Customers.Commands;

public class DeleteCustomerCommand
{
    public string CompanySlug { get; init; } = default!;
    public string CustomerSlug { get; init; } = default!;
    public Guid UserId { get; init; }
    public string ConfirmationName { get; init; } = default!;
}
