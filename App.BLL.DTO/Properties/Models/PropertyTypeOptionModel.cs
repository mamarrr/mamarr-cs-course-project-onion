namespace App.BLL.Contracts.Properties.Models;

public class PropertyTypeOptionModel
{
    public Guid Id { get; init; }
    public string Code { get; init; } = default!;
    public string Label { get; init; } = default!;
}
