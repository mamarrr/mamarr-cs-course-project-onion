namespace App.DAL.DTO.Lookups;

public class LookupDalDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = default!;
    public string Label { get; init; } = default!;
}
