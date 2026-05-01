namespace App.BLL.Contracts.Residents.Commands;

public class DeleteResidentCommand
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string ResidentIdCode { get; init; } = default!;
    public string ConfirmationIdCode { get; init; } = default!;
}
