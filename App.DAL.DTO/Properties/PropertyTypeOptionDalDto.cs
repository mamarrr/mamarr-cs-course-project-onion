namespace App.DAL.DTO.Properties;

public class PropertyTypeOptionDalDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = default!;
    public string Label { get; init; } = default!;
}
