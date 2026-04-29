namespace App.Contracts.DAL.Lookups;

public sealed class LookupDalDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = default!;
    public string Label { get; init; } = default!;
}
