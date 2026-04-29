namespace App.Contracts.DAL.Properties;

public sealed class PropertyTypeOptionDalDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = default!;
    public string Label { get; init; } = default!;
}
